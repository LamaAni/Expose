function nout = nargout_for_class(obj,funcName)
  mc=metaclass(obj);
  nout=-1;
  names={mc.MethodList.Name};
  outputNames={mc.MethodList.OutputNames};
  nameIdx=find(strcmp(names,funcName));
  if(~isempty(nameIdx))
      idx=nameIdx(1);
      nout=length(outputNames{idx});
  end
end