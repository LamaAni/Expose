classdef (ConstructOnLoad) ExposeLogEventStruct < event.EventData & handle
    %EXPOSELOGEVENTSTRUCT Summary of this class goes here
    %   Detailed explanation goes here
    
    properties
        Message='';
        CallerID='';
    end
    
    methods
        function obj = ExposeLogEventStruct(callerID,msg)
            obj.CallerID=callerID;
            if(exist('msg','var'))
                obj.Message=msg;
            end
        end
    end
end

