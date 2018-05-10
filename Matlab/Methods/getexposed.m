function [po] = getexposed(id)
    %MPORT get the Matlab port by its id.
    p=getexposedPort(id);
    po=p.PortObject;
end

