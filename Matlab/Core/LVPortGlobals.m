classdef LVPortGlobals < AutoRemoveAutoIDMap
    methods
        function obj = LVPortGlobals(url)
            % call base class constructor.
            obj@AutoRemoveAutoIDMap(LVPortGlobals.PortAutoRemoveTime); % 5 minutes.
            
            if(~exist('url','var'))
                url=CSCom.DefaultURL;
            end
            
            % generates and implemetns all the LVPort globals
            % that are required to identify ports by the thire names/ids
            obj.Server=CSCom(url);
            
            % adding listener for message events.
            obj.Server.addlistener('MessageRecived',@obj.OnMessageRecived);
            
            % adding listener for log events.
            obj.Server.addlistener('Log',@obj.OnLog);
            
            % setting up the server.
            %obj.Server.Listen();
        end
    end
    
    properties (SetAccess = protected)
        % global map with auto destroy.
        Server=[];
        PortsByID;
    end
    
    properties (Constant)
        PortAutoRemoveTime=5*60; % in seconds. 5 minutes.
    end
    
    methods
        % start the service if needed.
        function Init(obj)
            if(obj.Server.IsAlive)
                return;
            end
            obj.Server.Listen();
        end
        
        % refrence to self since this is the port collection.
        function [rt]=get.PortsByID(obj)
            rt=obj;
        end
        
        function [id]=Register(obj,p,id)
            id=obj.setById(id,p);
            p.ID=id;
        end
    end
    
    methods(Access = protected)
        function OnMessageRecived(obj,s,e)
            % finding message info.
            meta=strsplit(e.Message);
            portID=[];
            
            command=meta{1};
            if(length(meta)>1)
                portID=meta{2};
            end
            
            if(~isempty(portID))
                if(~obj.contains(portID))
                    warning(['Recived message for portid: ',portID...
                        ,', but the port was not found.']);
                end
                obj(portID).Invoke(command,e);
            else
               obj.OnCommand(command,e); 
            end
        end
        
        function OnCommand(obj,command,e)
            % general command translation.
            data=e.UpdateObject();
            switch(command)
                case "make"
                    if(~ischar(data))
                        warning('Called to create an LVPort but not codepath was sent.');
                        e.Response=false;
                        return;
                    end
                    [po,LVPort.MakePort(data)
                    %sLVPort.
                case "destroy"
            end
        end
        
        function OnLog(obj,s,e)
            disp(e.Data);
        end
    end
end

