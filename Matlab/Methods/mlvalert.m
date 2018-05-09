function mlvalert(p,msg)
    if(isa(p,'LVPort'))
    elseif(isa(p,'LVPortObject'))
        p=p.Port;
    elseif(ischar(p))
        p=mlvport(p);
    else
        error('Unrecognized port object when attempting to send an alert to user');
    end
    if(~exist('msg','var'))
        msg='';
    elseif(~ischar(msg))
        msg=matlab.unittest.diagnostics.ConstraintDiagnostic.getDisplayableString(msg);
    end
    p.PostEvent('malert',msg,'lvport_m');
end

