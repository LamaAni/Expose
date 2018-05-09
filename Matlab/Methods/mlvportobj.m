function [po] = mlvportobj(id)
    %MPORT get the Matlab port by its id.
    p=mlvport(id);
    po=p.PortObject;
end

