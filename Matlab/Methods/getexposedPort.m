function p = getexposedPort(id)
    %MPORT get the Matlab port by its id.
    if(~Expose.Global.contains(id))
        error(['A matlab port with id "',id,'" not found']);
    end
    p=Expose.Global(id);
end

