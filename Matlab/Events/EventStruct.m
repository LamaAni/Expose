classdef (ConstructOnLoad) EventStruct < event.EventData
    %EVENTSTRUCT General event data.
    %   Used as catch all event data.
    properties
        Data=[];
    end
    
    methods
        function [obj]=EventStruct(data)
            if(exist('data','var'))
                obj.Data=data;
            end
        end
    end
end

