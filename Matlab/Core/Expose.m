classdef Expose<handle
    %EXPOSE Creates a new expose object.
    methods
        function obj = Expose(com)
            if(~exist('com','var'))
                com=CSCom();
            end
            
            if(~isa(com,'ExposeCOM'))
                error('Exposition com must implement class ExposeCOM');
            end
            
            obj.Com=com;
            com.addlistener('MessageRecived',@obj.onMessage);
        end
    end
        
    properties (SetAccess = private)
        Com=[];
    end
    
    properties
        % override this property to allow for specific handler
        % manipulations.
        Handlers=[];
    end
    
    % messaging
    methods(Access = protected)
        function onMessage(obj,s,e)
            % events must be of type ExposeEventStruct.
            if(~isa(e,'ExposeMessageEventStruct'))
                error('Expose events must implement ExposeEventStruct');
            end

            % need to translate the message according to the required
            % parameters.
            switch(e.Message.MessageType)
                case ExposeMessageType.Error
                    error(e.Text);
                case ExposeMessageType.Warning
                    warning(e.Text);
                case ExposeMessageType.Create
                    obj.CreateHandler(e.CallerID,e);
                case ExposeMessageType.Destroy
                    obj.DestroyHandler(e.CallerID,e);
                case ExposeMessageType.Get
                    hndl=obj.GetHanlder(e.CallerID,e);
                    e.Response=e.Message.GetFrom(hndl);
                case ExposeMessageType.Set
                    hndl=obj.GetHanlder(e.CallerID,e);
                    e.Message.SetTo(hndl);
                case ExposeMessageType.Invoke
                    hndl=obj.GetHanlder(e.CallerID,e);
                    % in this case the text must be the 
                    % function name.
                    if(ismethod(hndl,e.Message.Text))
                        args=e.Message.Value();
                        nao=nargout_for_class(hndl,e.Message.Text);
                        if(~iscell(args))
                            args={args};
                        end
                        if(nao==1)
                            e.Response=hndl.(e.Message.Text)(args(:));
                        elseif(nao>1)
                            e.Response=cell(1,nao);
                            e.Response(:)=hndl.(e.Message.Text)(args(:));
                        else
                            hndl.(e.Message.Text)(args(:));
                        end
                    else
                        error(['Function "',e.Message.Text,'" not found. Function invoke failed.');
                    end
            end
        end
    end
    
    % public handler methods
    methods(Access = protected)
        function [o]=GetHandler(obj,id,e)
            if(isempty(obj.Handlers))
                o=obj;
                return;
            end
            
            o=obj.Handlers.GetHandler(id,e);
        end
        
        function [o]=CreateHandler(obj,id,e)
            if(isempty(obj.Handlers))
                o=obj;
                return;
            end
            
            o=obj.Handlers.CreateHandler(id,e);
        end
        
        function DestroyHandler(obj,id,e)
            if(isempty(obj.Handlers))
                % stop the connection since this is the self.
                obj.Com.Stop();
                return;
            end
            
            obj.Handlers.DestroyHandler(id,e);
        end
    end
end

