classdef ExposeCOM<handle
    %EXPOSECOM Defines the communication methods to be used in an expose
    %com connecton.
    events
        Log;
        MessageRecived;
    end
    
    methods (Abstract)
        Init(obj,url);
        Listen(obj);
        Connect(obj);
        Stop(obj);
    end
end

