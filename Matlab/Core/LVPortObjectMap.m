classdef LVPortObjectMap < handle
    %LVPORTOBJECMAP An object map that represents the state of an object.
    %an object map cannot be uploaded.
    
    methods
        function obj = LVPortObjectMap(omap,compareTo)
            obj.ValMap=omap;
            obj.ValInfoMap=containers.Map();
            if(exist('compareTo','var')&&isa(comapreTo,'containers.Map'))
                obj.removeUnchanged(compareTo,compareTo);
            end
        end        
    end
    
    properties (SetAccess = protected)
        ValMap=[]; % must be defined
        ValInfoMap=[];
    end
    
    methods        
        function [hasval,val,vsize,idxs,similarToOld]=GetNampathInfo(obj,namepath,ot)
            if(~exist('ot','var'))
                ot=[];
            end         
            hasval=0;
            val=[];
            vsize=[];
            idxs=[];
            if(~obj.ValMap.isKey(namepath))
                return;
            end
            val=obj.ValMap(namepath);
            if(~isempty(ot) && ischar(ot) && ~strcmp(ot,ObjectMap.getType(val)))
                val=[];
                return;
            end
            hasval=1;
            
            if(obj.ValInfoMap.isKey(namepath))
                idxsInfo=obj.ValInfoMap(namepath);
                idxs=idxsInfo.idxs;
                similarToOld=idxsInfo.similarToOld;
                
                if(~isempty(idxs))
                    vsize=idxsInfo.orgSize;
                    val=val(idxs);
                end
            else
                idxs=[];
                vsize=size(val);
            end
        end
        
        function minimizeUnchanged(obj,compareToMap)
            namepaths=compareToMap.keys;
            vals=compareToMap.values;
            
            for i=1:length(namepaths)
                namepath=namepaths{i};
                if(~obj.ValMap.isKey(namepath))
                    continue;
                end
                cval=vals{i};
                is_obj_similar=false;
                do_set_update_idxs=[];
                
                if(ismatrix(cval))
                    % must be the same size to compare.
                    if(all(size(cval)==size(obj.ValMap(namepath))))
                        % find index to update.
                        val=obj.ValMap(namepath);
                        do_set_update_idxs=find(cval~=val);
                        if(isempty(do_set_update_idxs))
                            is_obj_similar=true;
                        elseif(length(do_set_update_idxs)==length(val(:)))
                            do_set_update_idxs=[]; % they are all diffrent.
                        else
                            %disp(do_set_update_idxs);
                        end
                    end
                else
                    is_obj_similar= cval==obj.ValMap(namepath);
                end
                
                % we need to update?
                if(~isempty(do_set_update_idxs))
                    idxinfo={};
                    idxinfo.similarToOld=is_obj_similar;
                    idxinfo.idxs=do_set_update_idxs;
                    idxinfo.orgSize=size(cval);
                    obj.ValInfoMap(namepath)=idxinfo;
                end
            end
        end
    end
end

