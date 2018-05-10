classdef ExposePort < handle & ExposePortEvents & ExposePortProperties & ExposePortCom
    %PORT Holds a collection of methods, event postings and other about a
    %labview port object implementation.
    
    methods
        function [obj]=ExposePort(portObject)
            if(~exist('portObject','var'))
                portObject=ExposePortObject;
            end
            if(~isa(portObject,'ExposePortObject'))
                error('Port object must be derived from class "ExposePortObject"');
            end
            obj.PortObject=portObject;
        end
    end

    
    % main methods.
    properties (SetAccess = protected)
        PortObject=[];
        ID='';
    end
    
    properties (Constant)
        % global map with auto destroy.
        Global=ExposePortGlobals();%AutoRemoveAutoIDMap(5*60);
    end
    
    % global generation methods
    methods (Static)
        
        % call to initialize the Global service.
        function InitializeService()
            ExposePort.Global.Init();
        end

        % makes a new port from a codepath file.
        function [po,compileErrors]=MakePort(codepath,allowMultipleInstances)  
            po=[];
            
            compileErrors='';
            
            if(~exist('allowMultipleInstances','var'))
                allowMultipleInstances=false;
            end
            
            findCodeFIle=false;
            if(exist('codepath','var') && ischar(codepath))
                if(~endsWith(codepath,'.m'))
                    codepath=[codepath,'.m'];
                end
                findCodeFIle=true;
            end
            % create
            id=-1; % no sepcific class created. Use temp ids.
            if(findCodeFIle)
                try
                    if(~exist(codepath,'file'))
                        error(['File not found "',codepath,'"']);
                    end
                    
                    % get the compilation errors.
                    compileErrors=checkcode(codepath,'-string'); 
                    
                    % old version where a temp file is made.
                    % mew version uses the code as is.
%                     [className]=ExposePort.MakePortObjectTempCodeFile(codepath);
%                     compileErrors=checkcode(codepath,'-string');                    

                    % add the path
                    [fpath,className,~]=fileparts(codepath);
                    addpath(fpath);
                    
                    % create the objet.
                    po=eval(className);
                    if(~isa(po,'ExposePortObject'))
                        error('Port classes must derive from calss "PortObject"');
                    end
                catch err
                    compileErrors=[compileErrors,err.message];
                    return;
                end
                if(~allowMultipleInstances)
                    id=className;
                end
            else
                po=ExposePortObject();
            end
            
            ExposePort.Global.Register(po,id);
        end
%         
%         OLD VERSION USING TEMP FILES...
%
%
%         function [className]=MakePortObjectTempCodeFile(fpath,autoAccess)
%             if(~exist('autoAccess','var'))autoAccess=true;end
%             className=ExposePort.PathToLVID(fpath);
%             tempdir=[pwd,'\','LVTemp'];
%             if(~exist(tempdir,'file')) % check for folder.
%                 mkdir(tempdir);
%             end
%             
%             if(autoAccess)
%                 addpath(tempdir);% make sure we can access it.
%             end
%             
%             fname=[tempdir,'\',className,'.m'];
%             code=fileread(fpath);
%             code=regexprep(code,'(?<=classdef *)\w+',className,'ignorecase','once');
%             
%             % checking if exists, and if the same, then ignore write.
%             % otherwise delete old.
%             if(exist(fname,'file'))
%                 oldcode=fileread(fname);
%                 if(strcmp(oldcode,code))
%                     return;
%                 end
%                 delete(fname);
%             end
%             
%             % write all.
%             fid=fopen(fname,'a');
%             fprintf(fid,"%s",code);
%             fclose(fid);
%         end
%         
%         function [id]=PathToLVID(fpath)
%             id=['P',ExposePort_hash(lower(fpath)),'C'];
%         end
    end
end

