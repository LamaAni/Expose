classdef AutoRemoveMapRemoveEvent<EventStruct
    %AUTOREMOVEMAPREMOVEEVENT Summary of this class goes here
    %   Detailed explanation goes here
    
    properties (SetAccess = protected)
        element=[];
        id=[];
    end
    
    methods
        function obj = AutoRemoveMapRemoveEvent(el,id)
            obj.element=el;
            obj.id=id;
        end
    end
end

