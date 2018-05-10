classdef (ConstructOnLoad) CSComLogEventArgs < event.EventData & handle
    %CSCOMLOGEVENTARGS Summary of this class goes here
    %   Detailed explanation goes here
    
    properties
        Message
    end
    
    methods
        function obj = CSComLogEventArgs(msg)
            if(exist('msg','var'))
                obj.Message=msg;
            end
        end
    end
end

