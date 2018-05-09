classdef AutoRemoveAutoIDMap < AutoRemoveMap
    %AUTOREMOVETEMPOBJECTCOLLECTION Summary of this class goes here
    %   Detailed explanation goes here
    
    properties(SetAccess = private)
        LastTempAutoID=0;
    end
    
    methods
        function col = AutoRemoveAutoIDMap(autoRemoveTimeout)
            if(~exist('autoRemoveTimeout','var'))
                autoRemoveTimeout=60;
            end
            
            col@AutoRemoveMap(autoRemoveTimeout);
        end
        
        function [id]=NextTempID(obj)
            id=obj.LastTempAutoID;
            obj.LastTempAutoID=obj.LastTempAutoID+1;
        end
        
        % overriding to set the id.
        function [id]=setById(col,id,o) 
            if(~exist('id','var') || (isnumeric(id)&& id<0))
                id=col.NextTempID();
            end
            % call base.
            setById@AutoRemoveMap(col,id,o);
        end
    end
end

