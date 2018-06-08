classdef CSDelayedEventDispatch < handle
    %CSDelayedEventDispatch provides a C# threading delayed event 
    %execution.
    methods
        function obj = CSDelayedEventDispatch(assemblyLoc)
            if(isempty(which('CSCom.DelayedEventDispatch')))
                if(~exist('assemblyLoc','var'))
                    assemblyLoc=...
                        events.CSDelayedEventDispatch.GetAssemblyLocationFromCurrentFileLocation();
                end
                NET.addAssembly(assemblyLoc);
            end
            
            obj.NetO=CSCom.DelayedEventDispatch();
            obj.NetO.addlistener('Ready',@obj.onNetReady);
        end
    end
    
    properties(SetAccess = private)
        NetO=[];
    end
    
    events
        Ready;
    end
    
    methods
        function trigger(obj,delay,val)
            if(~exist('delay','var'))
                delay=0;
            end
            if(~exist('val','var'))
                val=[];
            end
            obj.NetO.Trigger(delay,val);
        end
    end
    
    methods (Access = private)
        function onNetReady(obj,s,e)
            try
                val=[];
                if(~isempty(e))
                    val=e.Value;
                end
                obj.notify('Ready', events.CSDelayedEventDispatchEventArgs(val));
            catch err
                warning(err);
            end
        end
    end
        
    methods(Static)
        function [apath]=GetAssemblyLocationFromCurrentFileLocation()
            fn=mfilename('fullpath');
            apath=fileparts(fileparts(fileparts(fn)));
            apath=[apath,'\COM\CSCom\CSCom\bin\Release\CSCom.dll'];
            %disp(apath);
        end
    end
end

