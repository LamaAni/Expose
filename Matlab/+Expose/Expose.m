classdef Expose < handle
    %EXPOSE Creates a new expose object.
    methods
        function obj = Expose(com)
            if(~exist('com','var'))
                com=Expose.CSCom.CSCom();
            end
            
            if(~isa(com,'Expose.Core.ExposeCOM'))
                error('Exposition com must implement class ExposeCOM');
            end

            obj.Com=com;
            com.addlistener('MessageRecived',@obj.onMessage);
            com.addlistener('Log',@obj.onLog);
        end
    end
        
    properties (SetAccess = private)
        Com=[];
    end
    
    properties
        % override this property to allow for specific handler
        % manipulations.
        IsAlive;
        IsListening;
        IsConnected;
        TraceLogs=false;
    end
    
    % general com methods
    methods
        function [rt]=get.IsAlive(exp)
            rt=exp.Com.IsAlive;
        end
        
        function [rt]=get.IsListening(exp)
            rt=exp.Com.IsListening;
        end
        
        function [rt]=get.IsConnected(exp)
            rt=exp.Com.IsConnected;
        end
        
        % listen for remote connections. (Blocks the port).
        function Listen(obj,varargin)
            if(obj.Com.IsAlive)
                error("This expose is already connected or listening.");
            end
            obj.Com.Init(varargin{:});
            obj.Com.Listen;
        end
        
        % connect to a remote and listen for connections.
        function Connect(obj,varargin)
            if(obj.Com.IsAlive)
                error("This expose is already connected or listening.");
            end
            obj.Com.Init(varargin{:});
            obj.Com.Connect();
        end
        
        % stop listenting/disconnect.
        function Stop(obj)
            if(obj.Com.IsAlive)
                obj.Com.Stop();
            end
        end
    end
    
    % messaging
    methods(Access = protected)
        function onLog(obj,s,e)
            % call to get the handler since we are logging.
            if(obj.TraceLogs)
                disp(e.Message);
            end
        end
        
        function onMessage(obj,s,e)
            % events must be of type ExposeEventStruct.
            import Expose.Core.*;
            if(~isa(e,'Expose.Core.ExposeMessageEventStruct'))
                error('Expose events must implement ExposeEventStruct');
            end
            
            % need to translate the message according to the required
            % parameters.
            switch(e.Message.MessageType)
                case ExposeMessageType.Error
                    error('Expose:ServerError',e.Message.Text);
                case ExposeMessageType.Warning
                    warning('Expose:ServerWarning',e.Message.Text);
                case ExposeMessageType.Create
                    obj.CreateHandler(e.CallerID,e);
                case ExposeMessageType.Destroy
                    obj.DestroyHandler(e.CallerID,e);
                case ExposeMessageType.Get
                    hndl=obj.GetHandler(e.CallerID,e);
                    e.Response=e.Message.GetFrom(hndl);
                case ExposeMessageType.Set
                    hndl=obj.GetHandler(e.CallerID,e);
                    e.Message.SetTo(hndl);
                case ExposeMessageType.Invoke
                    hndl=obj.GetHandler(e.CallerID,e);
                    % in this case the text must be the 
                    % function name.
                    if(ismethod(hndl,e.Message.Text))
                        args=e.Message.SetTo();
                        nao=nargout_for_class(hndl,e.Message.Text);
                        if(isempty(args))
                            args={};
                        elseif(~iscell(args))
                            args={args};
                        end
                        args{end+1}=e;
                        if(nao==1)
                            e.Response=hndl.(e.Message.Text)(args{:});
                        elseif(nao>1)
                            e.Response=cell(1,nao);
                            e.Response{:}=hndl.(e.Message.Text)(args{:});
                        else
                            hndl.(e.Message.Text)(args{:});
                        end
                    else
                        error(['Function "',e.Message.Text,'" not found. Function invoke failed.']);
                    end
            end
        end
    end
    
    % public handler methods
    methods(Access = protected)
        function [o]=GetHandler(obj,id,e)
            o=obj;
        end
        
        function [o]=CreateHandler(obj,id,e)
            o=obj;
        end
        
        function DestroyHandler(obj,id,e)
        end
    end
    
    methods
        function [varargout]=Invoke(exp,toID,name,varargin)
            import Expose.Core.*;
            if(~exist('name','var') && exist('toID','var'))
                name=toID;
                toID=[];
            end            
            if(~exist('toID','var'))
                toID=[];
            end
            hndl=exp.GetHandler(toID);
            if(~exist('name','var') || isempty(name) || ~ischar(name))
                error('Please provide a method name. (char array)');
            end
            
            args=varargin(:);
            if(isempty(args))
                args=[];
            elseif(length(args)==1)
                args=args{1};
            end
            
            if(nargout>0)
                varargout=cell(1,nargout);
                varargout{:}=exp.Com.Send(toID,name,...
                    ExposeMessageType.Invoke,args);
            else
                exp.Com.Send(toID,name,ExposeMessageType.Invoke,varargin(:),args);
            end
        end
        
        % calls to update the specific parameter to the remote value.
        function Update(exp,toID,name)
            import Expose.Core.*;
            if(~exist('name','var') && exist('toID','var'))
                name=toID;
                toID=[];
            end
            if(~exist('toID','var'))
                toID=[];
            end
            hndl=exp.GetHandler(toID);
            if(~exist('name','var'))
                name=fieldnames(hndl);
            end
            if(~exist('direct'))
                direct=false;
            end
            if(isempty(name))
                return;
            end
            if(~iscell(name))
                name={name};
            end
            hasValues=0;

            data=struct();
            for i=1:length(name)
                pn=name{i};
                if(~isprop(hndl,pn) && ~isfield(hndl,pn))
                    continue;
                end
                hasValues=1;
                data.(pn)=hndl.(pn);
            end
            if(~hasValues)
                return;
            end           
            exp.Com.Send(toID,name,ExposeMessageType.Set,data);
        end
        
        % retrives data from the client/server.
        function Get(exp,toID,name)
            import Expose.*;
        end
    end
end

