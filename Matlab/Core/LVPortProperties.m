classdef LVPortProperties < handle
    %LVPORTEVENTS INTERNAL OBJECT!! allows port to use properties update
    %system.
            
    methods
        function [obj]=LVPortProperties()
           obj.m_oldObjectCollection=AutoRemoveMap(5*60);
        end
    end
    
    properties
        UpdateChangesDefault=false;
        IgnorePropertiesOnUpdates={};
    end
    
    properties (Access = private)
        % a map for all partial update by name;
        m_oldObjectCollection=[];
    end
    
    properties (Constant)
        LVPortPropertiesEventCategory='lvport_m_properties';
    end
    
    methods
        
        % calls to invalidate the property and update it.
        function InvalidatePropertyByName(obj,name)
            if(obj.m_oldObjectCollection.contains(name))
                obj.m_oldObjectCollection.remove(name);
            end
            % call to update directly.
            obj.update(name);
        end
        
        function [evid]=update(obj,name,updateChanges)
            if(~exist('name','var'))
                name='';
            end
            if(~ischar(name))
                updateChanges=name;
                name='';
            end
            if(~exist('updateChanges','var'))
                updateChanges=obj.UpdateChangesDefault;
            end
            
            evid='';
            fnames=name;
            if(isempty(fnames))
                fnames=fieldnames(obj.PortObject);
            end
            
            if(iscell(fnames))
                if(length(fnames)>1)
                    evid=cell(length(fnames));
                    for i=1:length(fnames)
                        evid{i}=obj.update(fnames{i},updateChanges);
                    end
                    return;
                else
                    name=fnames{1};
                end
            end
            
            if(~isprop(obj.PortObject,name))
                return;
            end
            
            % check for ignores.
            if(any(contains(obj.IgnorePropertiesOnUpdates,name)))
                return;
            end
            
            evid=LVPortProperties.MakeEventIDFromParameterName(name);
            pID=obj.ID;
            up=@()LVPortProperties.globcaller_getObjectPropertyMapByName(...
                pID,name,updateChanges);
            % category matlab prop
            obj.PostEvent('mupdate',up,...
                LVPortProperties.LVPortPropertiesEventCategory,evid); 
        end
        
        function [map]=getObjectPropertyMapByName(obj,name,updateChanges)
            if(~exist('updateChanges','var'))
                updateChanges=obj.UpdateChangesDefault;
            end
            map=[];
            if(~isprop(obj.PortObject,name))
                return;
            end
            omap=ObjectMap.mapToCollection(obj.PortObject.(name));
            omap('~pname')=name; % an invalide map info to allow us to get the name without another temp.
            map=LVPortObjectMap(omap);
            if(updateChanges)
                if(obj.m_oldObjectCollection.contains(name))
                    map.minimizeUnchanged(obj.m_oldObjectCollection(name));
                end
                % set the new map.
                obj.m_oldObjectCollection(name)=omap;
            elseif(obj.m_oldObjectCollection.contains(name))
                obj.m_oldObjectCollection.remove(name);
            end
        end
    end
    
    methods (Access = private, Static)
        function [evid]=MakeEventIDFromParameterName(name)
            evid=['lvport_param_update:',name];
        end
    end
    
    methods (Static,Access = private)
        function [map]=globcaller_getObjectPropertyMapByName(pID,name,updateChanges)
            if(~mlvhasport(pID))
                % current port inactive?
                return;
            end
            p=mlvport(pID);
            map=p.getObjectPropertyMapByName(name,updateChanges);
        end        
    end
end

