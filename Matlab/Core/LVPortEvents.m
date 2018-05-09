classdef LVPortEvents < handle
    %LVPORTEVENTS INTERNAL OBJECT!! allows port to use event posting
    %system.
        
    methods
        function [obj]=LVPortEvents()
           obj.Events=AutoRemoveAutoIDMap(5*60); 
        end
    end
    
    properties
        Events=[];
    end
    
    properties (Access = private)
    end
    
    methods
        
        function [evid]=MakeNextAnonymousId(obj,namebase)
            evid=[namebase,num2str(obj.Events.NextTempID())];
        end
        
        function [evid]=PostEvent(obj,name,val,cat,evid)
            if(~exist('cat','var'))
                cat='';
            end
            if(~exist('evid','var'))
                evid=name;
            end
            
            ev=LVPortEventStruct(name,cat,val);
            obj.Events(evid)=ev;
        end
        
        function [evid]=PostAnonymousEvent(obj,name,val,cat)
            evid=obj.PostEvent(name,val,cat,obj.MakeNextAnonymousId(name));
        end
        
        function [ev]=GetPostedEvent(obj,evid)
            ev=[];
            if(obj.HasPostedEvent(evid))
                ev=obj.Events(evid);
            end
        end
        
        function [rt]=HasPostedEvent(obj,evid)
            rt=obj.Events.contains(evid);
        end
    end
    
    methods
        % pumps all the evnets and removes the current.
        function [evs]=PumpEvents(obj)
            evs=obj.Events.values;
            obj.ClearEvents();
        end
        
        % clear all events.
        function ClearEvents(obj)
            obj.Events.clear();
        end
        
        % returns current events.
        function [evs]=GetEvents(obj)
            evs=obj.Events.values;
        end
    end
end

