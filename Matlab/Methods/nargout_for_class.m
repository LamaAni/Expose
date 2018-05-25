function [nout,argnames] = nargout_for_class(obj,funcName)
  nout=-1;
  argnames={};
  
  mc=metaclass(obj);
  allmethodnames={mc.MethodList.Name};
  idx=find(strcmp(allmethodnames,funcName));
  if(isempty(idx))
      return;
  end
  moutputs={mc.MethodList.OutputNames};  
  argnames=moutputs{idx};
  nout=length(argnames);
end