classdef LVPortObject < handle
    %LVPORTOBJECT A calls for integrating matlab with labview.
    %   This class allows the integration of matlab with labview, by
    %   defining a global variable accesable by an ID, thus allowing the
    %   backend synchronization of labview parameters and methods.
    
    %   All synchronization of the port is kept in an info object (Port)
    %   that allows communication and event manipulation between labview
    %   and matlab.
    
    % the port item cannot should not be overriden or replaced.
    
    properties (SetAccess = private)
        Port=[];
    end
    
    properties (Access = private)
        m_port_refrence_object=[];
    end

    methods
        function [p]=get.Port(obj)
            if(isempty(obj.m_port_refrence_object))
                obj.m_port_refrence_object=LVPort(obj);
            end
            p=obj.m_port_refrence_object;
        end
        
        function update(obj,name,updateChanges)
            if(~exist('name','var'))
                name='';
            end
            if(~exist('updateChanges','var'))
                updateChanges=false;
            end            
            obj.Port.update(name,updateChanges);
        end
    end
end

