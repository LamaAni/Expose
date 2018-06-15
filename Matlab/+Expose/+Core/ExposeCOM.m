classdef ExposeCOM<handle
    %EXPOSECOM Defines the communication methods to be used in an expose
    %com connecton.
    events
        Log;
        Ping;
        MessageRecived;
    end
    
    properties(Abstract, SetAccess = protected)
        IsAlive;
        IsListening;
        IsConnected;
    end
    
    methods (Abstract)
        Init(obj,url);
        Listen(obj);
        Connect(obj);
        Stop(obj);
        [varargout]=Send(obj,toId,msg,mtype,data,requireResponse);
    end
end

