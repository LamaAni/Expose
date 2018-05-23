classdef testExposeClass < Expose.Expose
    %TESTEXPOSECLASS Summary of this class goes here
    %   Detailed explanation goes here
    methods
        function obj = testExposeClass(com)
            import Expose.CSCom.*;
            if(~exist('com','var'))
                com=CSCom();
            end
            obj@Expose.Expose(com);
        end
    end
    
    properties
        TestString='';
        TestMatrix=[1,2,3,4];
        TestNumeric=0;
        TestVector=[];
        TestStruct=[];
        MatrixSize=1000;
        MatrixUpdateTime=100; % in ms.
        TestGraph=[10,10;10,10];
    end
    
    methods
        function [rslt]=SomeMethod(exp)
            rslt=exp.Invoke([],'lama','Test data');
        end
        
        function [rslt]=GetSilentModeCount(exp)
            rslt=exp.Invoke([],'GetSilentModeCount');
        end
        
        function testing(exp)
            tic;
            n=201;
            for i=1:n
                exp.TestString=...
                    ['This is some updated string, with random num,',num2str(rand())];                
                exp.Update('TestString');
            end
            [cnt]=exp.GetSilentModeCount();
            total=toc;
            disp(['Updated ',num2str(n),' in ',num2str(total),' [ms]']);
        end
        
        function testGraphUpdate(exp,n,msize)

            if(~exist('n','var'))
                n=-1;
            end
            if(~exist('msize','var'))
                msize=exp.MatrixSize;
            end
            i=0;
            while(true)
                i=i+1;
                img=eye(msize)*3;
                img=img+rand(msize);
                img=img(10:end-10,:);
                exp.TestGraph=double(256*(img./max(img(:))));
                exp.Update('TestGraph');
                pause(exp.MatrixUpdateTime/1000);
                if(n>0 && i>=n)
                    break;
                end
            end
        end
    end
    
    methods(Static)
        function [exp]=doServerTest()
            exp=Expose.Testing.testExposeClass();
            exp.Com.TraceLogs=true;
            exp.Listen();
        end
    end
end

