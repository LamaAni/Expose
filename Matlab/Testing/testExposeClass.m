classdef testExposeClass < Expose
    %TESTEXPOSECLASS Summary of this class goes here
    %   Detailed explanation goes here
    methods
        function obj = testExposeClass(com)
            if(~exist('com','var'))
                com=CSCom();
            end
            obj@Expose(com);
        end
    end
    
    properties
        TestString='';
        TestMatrix=[1,2,3,4];
    end
    
    methods
        function [rslt]=someInvokeMethod(obj,o)
            disp(o);
            rslt=32;
        end
    end
    
    methods(Static)
        function [exp]=doServerTest()
            exp=testExposeClass();
            exp.Com.TraceLogs=true;
            exp.Listen();
        end
    end
end

