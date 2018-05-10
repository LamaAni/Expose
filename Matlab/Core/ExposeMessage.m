classdef ExposeMessage<handle
    %EXPOSEMESSAGE Implements the methods and properties of 
    % and expose message. The expose message is a handle since it is
    % transferred many times.
    
    properties
        % default message type is a warning.
        MessageType=ExposeMessageType.Warning;
        % Some text that accompanies the message, usefule for simple
        % messaging structures.
        Text=[];
    end
    
    methods(Abstract)
        Value(obj); % gets the value/s of the message
        SetTo(obj,to);% updates 'toUpdate' to reflect the object changes.
        GetFrom(obj,from); % returns the values of the object from, according to the message.
    end
end

