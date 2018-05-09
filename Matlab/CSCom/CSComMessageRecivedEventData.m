classdef (ConstructOnLoad) CSComMessageRecivedEventData < event.EventData & handle
    %EVENTSTRUCT General event data.
    %   Used as catch all event data.
    properties (SetAccess = private)
        Message=[];
        RequiresResponse=false;
        WebsocketID='';
    end
    
    properties
        Response=[];
    end
    
    methods
        function [obj]=CSComMessageRecivedEventData(msg,requiresResponse, websocketID)
            obj.Message=msg;
            obj.RequiresResponse=requiresResponse;
            obj.WebsocketID=websocketID;
        end
    end
end

