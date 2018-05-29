classdef (ConstructOnLoad) CSDelayedEventDispatchEventArgs < event.EventData & dynamicprops
    %EVENTSTRUCT General event data.
    %   Used as catch all event data.
    
    methods
        function [obj]=CSDelayedEventDispatchEventArgs(data)
            if(exist('data','var'))
                obj.Data=data;
            end
        end
    end
    
    properties
        Data=[];
    end
end

