classdef ExposeHandlerCollection<handle
    %EXPOSEOBJECTCOLLECTION Summary of this class goes here
    %   Detailed explanation goes here 
    methods(Abstract)
        GetHandler(obj,id,e);
        CreateHandler(obj,id,e);
        DestroyHandler(obj,id,e);
    end
end

