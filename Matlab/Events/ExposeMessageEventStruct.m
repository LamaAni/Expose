classdef (ConstructOnLoad) ExposeMessageEventStruct < event.EventData & handle
    %LVPORTEVENTSTRUCT Summary of this class goes here
    %   Detailed explanation goes here

    methods
        function ev=ExposeMessageEventStruct(callerID,msg,requireResponse)  
            % messages should be of type ExposeMessage.
            if(isa(callerID,'string'))
                callerID=char(callerID);
            end
            if(~ischar(callerID))
                error('You must provide a string based caller id.');
            end
            
            if(~isa(msg,'ExposeMessage'))
                error('Exposure messages must implement ExposeMessage');
            end
            
            if(~islogical(requireResponse))
                error('Require response must be either false or true.');
            end
            
            ev.CallerID=callerID;
            ev.Message=msg;
            ev.RequiresResponse=requireResponse;
        end
    end
    
    properties(SetAccess = protected)
        % the id of the caller.
        CallerID='';
        Message=[];
        RequiresResponse=false;
    end    
    
    properties
        Response=[];
    end
end

