classdef CSComMessageType<int32
    enumeration
        Error(1)
        Warning(2)
        Create(4)
        Destroy(8)
        Invoke(16)
        Get(32)
        Set(64)
    end
end

