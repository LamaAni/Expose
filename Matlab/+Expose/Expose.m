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
            obj.asyncUpdateEventDispatch=events.CSDelayedEventDispatch();
            obj.asyncUpdateEventDispatch.addlistener('Ready',...
                @obj.onAsyncUpdateEventDispatchReady);
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
            if(~isvalid(obj)||obj.TraceLogs)
                disp(e.Message);
            end
        end
        
        function onMessage(obj,s,e)
            if(~isvalid(obj))
                return;
            end
            % events must be of type ExposeEventStruct.
            import Expose.Core.*;
            if(~isa(e,'Expose.Core.ExposeMessageEventStruct'))
                error('Expose events must implement ExposeEventStruct');
            end
            
            %disp([e.CallerID,': msgT->',num2str(e.Message.MessageType)]);
            
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
                        nai=nargin_for_class(hndl,e.Message.Text);
                        if(isempty(args))
                            args={};
                        elseif(~iscell(args))
                            args={args};
                        end
                        args{end+1}=e;
                        if(length(args)>nai)
                            args=args(1:nai);
                        end
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
            if(isempty(hndl))
                warning('Empty handler returned for invoke message.');
                return;
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
        function Update(exp,toID,name,direct)
            import Expose.Core.*;
            if(~exist('name','var') && exist('toID','var'))
                name=toID;
                toID=[];
            end
            if(~exist('toID','var'))
                toID=[];
            end
            hndl=exp.GetHandler(toID);
            if(isempty(hndl))
                disp(['Empty handler found for id: ',toID,', update ignored.']);
                return;
            end
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
            
            if(~direct)
                if(isempty(exp.pendingUpdates))
                    exp.pendingUpdates=containers.Map;
                end
                if(~exp.pendingUpdates.isKey(toID))
                    exp.pendingUpdates(toID)=name;
                else
                    conc=exp.pendingUpdates(toID);
                    conc(end+1:end+length(name))=name;
                    exp.pendingUpdates(toID)=conc;
                end
                exp.asyncUpdateEventDispatch.trigger(1);
                return;
            end

            data=struct();
            name=unique(name);
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
            if(~exist('name','var') && exist('toID','var'))
                name=toID;
                toID=[];
            end
            if(~exist('toID','var'))
                toID=[];
            end
            hndl=exp.GetHandler(toID);
            if(isempty(hndl))
                warning('Empty handler returned for get message.');
                return;
            end
            if(~exist('name','var'))
                name=fieldnames(hndl);
            end
            if(isempty(name))
                return;
            end
            if(~iscell(name))
                name={name};
            end            
        end
    end
    
    properties(Access = private)
        asyncUpdateEventDispatch=[];
        pendingUpdates=[];
    end
    
    methods(Access = private)
        function onAsyncUpdateEventDispatchReady(obj,s,e)
            if(isempty(obj.pendingUpdates))
                return;
            end
            updates=obj.pendingUpdates;
            obj.pendingUpdates=[];
            ids=updates.keys;
            names=updates.values;
            for i=1:length(ids)
                id=ids{i};
                obj.Update(id,names{i},true);
            end
        end
    end
end

