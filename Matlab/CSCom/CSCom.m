classdef CSCom < ExposeCOM
    %CSCOM Implements the websocket connection module to be used
    methods
        function obj = CSCom(assemblyLoc)
            if(isempty(which('CSCom.CSCom')))
                if(~exist('assemblyLoc','var'))
                    assemblyLoc=CSCom.GetAssemblyLocationFromCurrentFileLocation();
                end
                NET.addAssembly(assemblyLoc);
            end
        end
    end

    
    % abstract class implementations
    properties(SetAccess = private)
        Url=[];
    end
    
    methods
        function Init(obj,url)
            if(~exist('url','var'))
                url=CSCom.DefaultURL;
            end
            
            obj.Url=url;
            obj.NetO=CSCom.CSCom(url);
            obj.NetO.DoLogging=true;
            obj.NetO.addlistener('Log',@obj.onLog);
            obj.NetO.addlistener('MessageRecived',@obj.onMessage);            
        end
        
        function Listen(obj)
            obj.NetO.Listen();
        end
        
        function Connect(obj)
            obj.NetO.Connect();
        end
    end    
    
    properties(Constant)
        DefaultURL='ws://localhost:50000/CSCom';
    end
    
    properties
        TraceLogs=false;
    end
    
    properties (SetAccess = private)
        NetO=[];
        LastResponceIndex=0;
        IsAlive=[];
        
    end
    
    methods
        function [rt]=get.IsAlive(obj)
            rt=obj.NetO.IsAlive;
        end
        
        function Stop(obj)
            obj.NetO.Stop();
        end
        
        function [varargout]=Send(obj,toId,msg,data,requireResponse)
            % making the message from the map.
            wasComMessage=true;
            if(~isa(msg,'CSComMessage'))
                wasComMessage=false;
                if(~exist('data','var'))
                    data=[];
                end
                msg=CSComMessage(msg,CSComMessageType.Data,data);
            end
            if(~exist('requireResponse','var'))
                requireResponse=nargout>0;
            end
            rsp=obj.NetO.Send(msg.ToNetObject(),requireResponse,toId);
            if(~requireResponse)
                return;
            end
            % moving to matlab.
            rsp=CSComMessage.FromNetObject(rsp);
            if(wasComMessage)
                varargout(1)=rsp;
                return;
            end
            
            % converting back to object.
            ro=rsp.UpdateObject();
            
            % checking the number of argumetns.
            if(nargout>1 && iscell(ro))
                varargout(:)=ro;
            else
                varargout(1)=ro;
            end
        end
        
        function NotifyError(obj,toID,msg)
            if(~exist('msg','var'))
                msg=toID;
                toID='';
            end
            msg=CSComMessage(msg,CSComMessageType.Error);
            obj.Send(toID,msg);
        end
        
        function NotifyWarning(obj,toID,msg)
            if(~exist('msg','var'))
                msg=toID;
                toID='';
            end
            msg=CSComMessage(msg,CSComMessageType.Warning);
            obj.Send(toID,msg);
        end
    end
    
    methods(Access = protected)
        function onLog(obj,s,e)
            msg=e.Message;
            obj.notify('Log',ExposeLogEventStruct(msg));
            if(obj.TraceLogs)
                disp(msg);
            end
        end
        
        function onMessage(obj,s,e)
            if(~event.hasListener(obj,'MessageRecived'))
                return;
            end
            
            try
                msg=CSComMessage.FromNetObject(e.Message);
                
                lastwarn(''); %reset the state of the last warning.
                
                % call the event.
                obj.notify('MessageRecived',ExposeMessageEventStruct(msg,...
                    e.WebsocketID,e.RequiresResponse));

                [emsg,wrnid]=lastwarn;
                haserror=false;
                haswarning=~isempty(wrnid);
                if(haswarning)
                    haserror=contains(wrnid,'error');
                    haswarning=false;
                end
                if(e.RequiresResponse)
                    if(~haserror)
                        obj.Send(e.WebsocketID,"response",e.Response);
                    else
                        obj.NotifyError(e.WebsocketID,emsg);
                    end
                    
                    if(haswarning)
                        obj.NotifyWarning(e.WebsocketID,emsg);
                    end
                else
                    if(haserror)
                        obj.NotifyError(e.WebsocketID,emsg);
                    elseif(haswarning)
                        obj.NotifyWarning(e.WebsocketID,emsg);
                    end
                end
            catch err
                % send the error back to the server.
                obj.NotifyError(e.WebsocketID,err.message);
                % warn about the error.
                warning(err.message);
            end
        end
    end
    
    methods
        function delete(obj)
            obj.NetO.Dispose();
            obj.NetO=[];
        end
    end

    methods(Static)
        function [val]=NetValueToRawData(nval)
            val=[];
            vc=class(nval);
            vc=lower(vc);
            if(contains(vc,'.boolean'))
                val=logical(nval);
            elseif(contains(vc,'.double'))
                val=double(nval);
            elseif(contains(vc,'.single'))
                val=single(nval);
            elseif(contains(vc,'.sbyte'))
                val=int8(nval);
            elseif(contains(vc,'.byte'))
                val=uint8(nval);
            elseif(contains(vc,'.uint16'))
                val=uint16(nval);
            elseif(contains(vc,'.int16'))
                val=int16(nval);
            elseif(contains(vc,'.uint32'))
                val=uint32(nval);
            elseif(contains(vc,'.int32'))
                val=int32(nval);
            elseif(contains(vc,'.uint64'))
                val=uint64(nval);
            elseif(contains(vc,'.int64'))
                val=int64(nval);
            elseif(contains(vc,'.char'))
                val=char(nval);
            elseif(contains(vc,'.string')) % string will become char.
                val=char(nval);
            end
        end
    end
    
    methods(Static)
        function [apath]=GetAssemblyLocationFromCurrentFileLocation()
            fn=mfilename('fullpath');
            apath=fileparts(fileparts(fileparts(fn)));
            apath=[apath,'\COM\CSCom\CSCom\bin\Release\CSCom.dll'];
            %disp(apath);
        end
    end
    
    %%%%%%%%%%%%%%%%%%%%%%%%%%
    % Testing
    
    methods(Static)
        function [server]=testAsServer()
            server=CSCom();
            server.TraceLogs=true;
            server.addlistener('MessageRecived',@CSCom.testAsServerMessageListener);
            server.addlistener('Log',@(s,e)disp(e.Message));
            server.Listen();
        end
        
        function testAsServerMessageListener(s,e)
        end
    end
end

