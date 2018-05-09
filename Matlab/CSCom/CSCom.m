classdef CSCom < handle
    %CSCOM Implements the websocket connection module to be used
    methods
        function obj = CSCom(url,assemblyLoc)
            if(~exist('url','var'))
                url=CSCom.DefaultURL;
            end
            if(isempty(which('CSCom.CSCom')))
                if(~exist('assemblyLoc','var'))
                    assemblyLoc=CSCom.GetAssemblyLocationFromCurrentFileLocation();
                end                
                NET.addAssembly(assemblyLoc);
            end
            NetO=CSCom.CSCom(url);
            NetO.addlistener('Log',@obj.onLog);
            NetO.addlistener('MessageRecived',@obj.onMessage);
        end
    end
    
    properties(Constant)
        DefaultURL='ws://localhost:50001/CSCom';
    end
    
    properties
        ShowLogs=false;
    end
    
    properties (SetAccess = private)
        NetO=[];
        LastResponceIndex=0;
        IsAlive=[];
    end
    
    events
        Log;
        MessageRecived;
    end
    
    methods
        
        function [rt]=get.IsAlive(obj)
            rt=obj.NetO.IsAlive;
        end
        
        function Listen(obj)
            obj.NetO.Listen();
        end
        
        function Connect(obj)
            obj.NetO.Connect();
        end
        
        function Stop(obj)
            obj.NetO.Stop();
        end
        
        function [varargout]=Send(obj,msg,mtype,o)
            % making the message from the map.
            wasComMessage=true;
            if(~isa(msg,'CSComMessage'))
                wasComMessage=false;
                msg=CSComMessage.FromMatlabObj(msg,mtype,o);
            end
            requireResponse=nargout>0;
            rsp=obj.NetO.Send(msg.ToNetObject(),requireResponse);
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
    end
    
    methods(Access = protected)
        function onLog(obj,s,e)
            msg=e.Message;
            obj.notify('Log',EventStruct(msg));
            if(obj.ShowLogs)
                disp(msg);
            end
        end
        
        function onMessage(obj,s,e)
            if(~event.hasListener(obj,'MessageRecived'))
                return;
            end
            
            msg=CSComMessage.FromNetObject(...
                e.Message);
            
            obj.notify('MessageRecived',CSComMessageRecivedEventData(msg,...
                e.WebsocketID,e.RequiresResponse));
            
            if(e.RequiresResponse)
                obj.Send(e.Response);
            end
        end
    end

    methods(Static)
        function [val]=NetValueToRawData(nval)
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
            apath=fileparts(fileparts(fileparts(fileparts(fn))));
            apath=[apath,'\CSCom\CSCom\bin\Release\CSCom.dll'];
            %disp(apath);
        end
    end
end

