classdef ExposeMessageType<int32
    %ExposeMessageType the type of the message to be translated.
    enumeration
        Error(1)
        Warning(2)
        Create(4)
        Destroy(8)
        Invoke(16)
        Get(32)
        Set(64)
        Response(128)
    end
end

