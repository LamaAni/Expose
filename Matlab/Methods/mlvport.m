function p = mlvport(id)
    %MPORT get the Matlab port by its id.
    if(~LVPort.Global.contains(id))
        error(['A matlab port with id "',id,'" not found']);
    end
    p=LVPort.Global(id);
end

