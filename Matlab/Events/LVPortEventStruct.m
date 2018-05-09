classdef LVPortEventStruct < handle
    %LVPORTEVENTSTRUCT Summary of this class goes here
    %   Detailed explanation goes here
    
    properties
        Name=[];
        Category='';
        Value=[];
    end
    
    methods
        
        function ev=LVPortEventStruct(name, cat, val)
            if(exist('name','var'))ev.Name=name;end
            if(exist('cat','var'))ev.Category=cat;end
            if(exist('val','var'))ev.Value=val;end
        end
    end
end

