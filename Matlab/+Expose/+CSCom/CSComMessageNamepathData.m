classdef CSComMessageNamepathData < handle
    %CSCOMMESSAGENAMEPATHDATA Summary of this class goes here
    %   Detailed explanation goes here
    
    properties
       %Idxs=int32([]);
       %Size=int32([]);
       Value;
       Namepath;
    end
    
    methods
        function [obj]=CSComMessageNamepathData(namepath,value,vsize,idxs)
            if(~ischar(namepath))
                if(isa(namepath,'string'))
                   namepath=char(namepath);
                else
                    error('Namepath must be a char value.');
                end
            end
            obj.Namepath=namepath;
            obj.Value=value;
        end
        
        function [val]=GetValue(obj,org)
            val=obj.Value;
            
            % nothing to do if not a value.
%             if(~ismatrix(val))
%                 return;
%             end
            
%             % found updating indexs.
%             if(~isempty(obj.Idxs) && ismatrix(org) && numel(org)==numel(val))
%                 org=org(:);
%                 org(obj.Idxs)=val;
%                 val=org;
%             end
%             
%             % reshaping the new value.
%             if(~isempty(obj.Size))
%                 val=reshape(val(:),obj.Sizes);
%             end
        end
    end
    
    methods(Static)
        function [map]=ToExposeMapperMap(vals)
            if(isa(vals,containers.Map))
                vals=vals.values;
            end
            map=containers.Map();
            for i=1:length(vals)
                map(vals{i}.Namepath)=vals{i};
            end
        end
        
        function [map]=ToNamepathDataMap(map,compareTo)
            import Expose.Core.*;
            import Expose.Map.*;
            import Expose.CSCom.*;
            
            if(~isa(map,'containers.Map'))
                map=ExposeMapper.mapToCollection(map);
            end
            hasCompTo=0;
            if(exist('compareTo','var'))
                hasCompTo=1;
                if(~isa(compareTo,'containers.Map'))
                 compareTo=ExposeMapper.mapToCollection(compareTo);
                end
            end
            namepaths=map.keys;
            
            for i=1:length(namepaths)
                namepath=namepaths{i};
                val=map(namepath);
                
                if(hasCompTo && compareTo.isKey(namepath))
                    npd=CSComMessageNamepathData.FromValue(namepath,val,compareVal);
                else
                    npd=CSComMessageNamepathData.FromValue(namepath,val);
                end
                if(isempty(npd))
                    map.remove(namepath);
                else
                    map(namepath)=npd;
                end
            end
        end
        
        function [npd]=FromValue(namepath,value,compareTo)
            import Expose.Core.*;
            import Expose.Map.*;
            import Expose.CSCom.*;
            
            if(isa(value,'CSComMessageNamepathData'))
                % in the case we already have a namepath data.
                if(~exist('compareTo','var'))
                    % recalculate compare.
                    value=value.Value;
                else
                    % nothing to do.
                    npd=value;
                    return;
                end
            end
            
            npd=CSComMessageNamepathData(namepath,value);
            
            % removed since partial update is not supported yet.
%             npd=[];
%             idxs=[];
%             vsize=[];
%             ispartial=0;
%             if(exist('compareTo','var') &&...
%                     ismatrix(value) && ismatrix(compareTo)&&...
%                     all(size(value)==size(compareTo)) &&...
%                     isa(value,class(compareTo)))
%                 % update only partial.
%                 idxs=find(value(:)~=compareTo(:));
%                 
%                 % if all elements then not partial.
%                 ispartial=length(idxs)==numel(value);
%             end
%             
%             if(ismatrix(value))
%                 vsize=size(value);
%             end
%             if(ispartial && isempty(idxs))
%                 % nothing to update.
%                 return;
%             end
%             npd=CSComMessageNamepathData(namepath,value,vsize,idxs);
        end
    end
end

