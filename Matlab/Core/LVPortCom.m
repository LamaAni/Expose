classdef LVPortCom < handle
    %LVPORTCOM INTERNAL!! Methods for communication 
    
    % construction
    methods
        function [obj]=LVPortCom()
        end
    end
    
    properties (SetAccess = private)
        ComInitialized=false;
        ComId='';
    end
    
    methods (Access = protected)     
        function [id]=InitializeRemote(p,id,comId)
            id=LVPort.Global.setById(id,p);
            p.ID=id;
            p.ComInitialized=true;
            p.ComId=comId;
        end
    end
    
    properties (Access = protected)
        % a collection of temporary objects to use for the mapping
        % of the object value.
        TempObjects=[]; % auto remove timeout is 1 minute.
        LastTempAutoID=0;
        ObjectHasLoopMethod=[];
        LoopEventInfo=EventStruct();
    end
    
    events
        Loop;
    end

    % temp value methods
    methods
        function RemoveTempObject(obj,id)
            obj.TempObjects.removeById(id);
        end
        
        function [id]=SetTempObject(obj,id,o)
            if(~exist('o','var'))
                o=id;
                id=-1;
            end
            ot=struct();
            ot.obj=o;
            ot.map=[];
            id=obj.TempObjects.setById(id,ot);
        end
        
        function [o]=GetTempObject(obj,id)
            o=[];
            map=[];
            if(~obj.TempObjects.contains(id))
                return;
            end
            to=obj.TempObjects(id);
            o=to.obj;
        end
    end
    
    
    % namepath methods
    methods
        function [wasRemoved]=ClearTemp(obj,id)
            id=obj.validateObjectID(id);
            if(~obj.TempObjects.contains(id))
                return;
                wasRemoved=0;
            end
            obj.TempObjects.remove(id);
            wasRemoved=1;
        end
        
        function [namepaths,otypes]=GetMapInfo(obj,id)
            id=obj.validateObjectID(id);
            namepaths='';
            otypes='';
            if(~obj.TempObjects.contains(id))
                return;
            end
            ot=obj.validateObjectTempMap(id,obj.TempObjects(id));
            namepaths=strjoin(ot.map.ValMap.keys,'!');
            lvals=length(ot.map.ValMap.keys);
            vals=ot.map.ValMap.values;
            otypes=cell(1,lvals);
            for i=1:lvals
                otypes{i}=ObjectMap.getType(vals{i});
            end
            otypes=strjoin(otypes,'!');
        end
        
        function [val,hasval,vsize,idxs]=GetNamepathValue(obj,id,namepath,otype)
            if(~exist('otype','var'))
                otype=[];
            end
            id=obj.validateObjectID(id);
            hasval=0;
            val=[];
            vsize=[0,0];
            idxs=[];
            if(~obj.TempObjects.contains(id))
                return;
            end
            ot=obj.validateObjectTempMap(id,obj.TempObjects(id));
            [hasval,val,vsize,idxs]=ot.map.GetNampathInfo(namepath,otype);
            
            if(isnumeric(val))
                if(~isempty(idxs))
                    if(numel(idxs)==prod(vsize))
                    % number of indexs matches
                        idxs=[];
                    else
                        idxs=obj.convertToLabviewIndexs(idxs,vsize);
                        idxs=idxs(:)';
                    end
                end
                
                % for labview read.
                val=val(:)';
                vsize=vsize(end:-1:1); 
            else
                % will always be true?
                idxs=[];
            end
            
            % labview required conversions.
            idxs=idxs-1; % to zero based indexs.
        end
        
        function [idxs]=convertToLabviewIndexs(obj,idxs,vsize)
            % easy way for now... but this is slow.
%             m=zeros(vsize);
%             m(idxs)=1;
%             m=reshape(m,vsize(end:-1:1));
%             idxs=find(m==1);
        end
        
        function [isok]=SetNamepathValue(obj,id,namepath,val)
            id=obj.validateObjectID(id);
            isok=0;
            if(~obj.TempObjects.contains(id))
                return;
            end
            ot=obj.TempObjects(id);
            [ot.obj,isok]=ObjectMap.update(ot.obj,namepath,val);
            obj.TempObjects(id)=ot;
        end
    end
        
    properties(Constant)
        LVPortCom_PortObjectID=-2;
    end
    
    % direct set namepath values to object properties.
    methods (Access = private)
        function [ot]=validateObjectTempMap(obj,id,ot)
            if(isempty(ot.map))
                % making the map;
                if(isa(ot.obj,'function_handle'))
                    ot.map=ot.obj();
                    if(~isa(ot.map,'LVPortObjectMap'))
                        ot.map=LVPortObjectMap(ObjectMap.mapToCollection(ot.map));
                    end
                else
                    ot.map=LVPortObjectMap(ObjectMap.mapToCollection(ot.obj));
                end
                obj.TempObjects(id)=ot;
            end
        end
        
        function [id]=validateObjectID(obj,id)
            switch id
                case LVPort.LVPortCom_PortObjectID
                    id='__portobject';
                    if(~obj.TempObjects.contains(id))
                        obj.SetTempObject(id,obj.PortObject);
                    end
            end
        end
    end
    
    % com methods.
    methods
        % call to pump all messages into temp objects
        % and return the ids with the message names.
        function [mnames,mcats,mtempids]=PumpMessages(obj)
            % check for loop method.
            if(isempty(obj.ObjectHasLoopMethod))
                obj.ObjectHasLoopMethod=ismethod(obj.PortObject,'loop');
            end
            
            if(obj.ObjectHasLoopMethod)
                obj.PortObject.loop();
            end

            if(event.hasListener(obj,'Loop'))
                obj.notify('Loop',obj.LoopEventInfo);
            end
            
            % get all pending events.
            evs=obj.PumpEvents();
            levs=length(evs);
            mnames=cell(1,levs);
            mcats=cell(1,levs);
            mtempids=ones(1,levs)*(-1);
            for i=1:length(evs)
                ev=evs{i};
                mnames{i}=ev.Name;
                mcats{i}=ev.Category;
                mtempids(i)=obj.SetTempObject(ev.Value);
            end
            
            mnames=strjoin(mnames,'!');
            mcats=strjoin(mcats,'!');
        end
    end
    
    % method invoke methods
    methods
        % invcokes a method and returns its temp value id (or -1).
        function [isok,outargsTempID]=InvokeMethod(obj,name,inargsTempID)
            isok=0;
            outargsTempID=-1;
            if(~ismethod(obj.PortObject,name))
                return;
            end
            
            if(inargsTempID>-1)
                iargs=obj.GetTempObject(inargsTempID);
            else
                iargs={};
            end
            
            nargs=lvport_nargout_for_class(obj.PortObject,name);
            if(nargs==0)
                exp.(name)(iargs{:});
                return;
            end
            
            if(~iscell(iargs))
                iargs={iargs};
            end         
            aout=cell(nargs,1);
            [aout{:}]=obj.PortObject.(name)(iargs{:});
            if(length(aout)==1)
                aout=aout{1};
            end
            
            outargsTempID=obj.SetTempObject(aout);
            isok=1;
        end
    end
end

