function [nin,argnames] = nargin_for_class(obj,funcName)
  nin=-1;
  argnames={};
  
  mc=metaclass(obj);
  allmethodnames={mc.MethodList.Name};
  idx=find(strcmp(allmethodnames,funcName));
  if(isempty(idx))
      return;
  end
  mstatics={mc.MethodList.Static};
  minputs={mc.MethodList.InputNames};  
  
  idx=idx(1);
  isStatic=mstatics{idx};
  isStatic=isStatic(1)==1;
  argnames=minputs{idx};
  if(~isStatic)
      % skip the class object.
      argnames=argnames(2:end);
  end  
  nin=length(argnames);

end