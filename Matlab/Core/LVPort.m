classdef LVPort < handle & LVPortEvents & LVPortProperties & LVPortCom
    %PORT Holds a collection of methods, event postings and other about a
    %labview port object implementation.
    
    methods
        function [obj]=LVPort(portObject)
            if(~exist('portObject','var'))
                portObject=LVPortObject;
            end
            if(~isa(portObject,'LVPortObject'))
                error('Port object must be derived from class "LVPortObject"');
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
        Global=LVPortGlobals();%AutoRemoveAutoIDMap(5*60);
    end
    
    % global generation methods
    methods (Static)
        
        % call to initialize the Global service.
        function InitializeService()
            LVPort.Global.Init();
        end
        
        % makes a new port from a codepath file.
        function [po,pathFound,compileErrors]=MakePort(codepath)  
            pathFound=false;
            compileErrors='';
            id=-1;
            if(exist('codepath','var') && ischar(codepath))
                if(~endsWith(codepath,'.m'))
                    codepath=[codepath,'.m'];
                end
                pathFound=true;
            end
            % create
            po=[]; % just to make sure we know it;
            if(pathFound)
                try
                    if(~exist(codepath,'file'))
                        pathFound=false;
                        error(['File not found "',codepath,'"']);
                    end
                    
                    [className]=LVPort.MakePortObjectTempCodeFile(codepath);
                    compileErrors=checkcode(codepath,'-string');                    
                    
                    po=eval(className);
                    if(~isa(po,'LVPortObject'))
                        error('Port classes must derive from calss "PortObject"');
                    end
                catch err
                    compileErrors=[compileErrors,err.message];
                    id='-1';
                    return;
                end
                id=className;
            else
                po=LVPortObject();
            end
            
            [id]=LVPort.Global.Register(po,id);
            
            if(isnumeric(id))
                id=num2str(id);
            end
        end
        
        function [className]=MakePortObjectTempCodeFile(fpath,autoAccess)
            if(~exist('autoAccess','var'))autoAccess=true;end
            className=LVPort.PathToLVID(fpath);
            tempdir=[pwd,'\','LVTemp'];
            if(~exist(tempdir,'file')) % check for folder.
                mkdir(tempdir);
            end
            
            if(autoAccess)
                addpath(tempdir);% make sure we can access it.
            end
            
            fname=[tempdir,'\',className,'.m'];
            code=fileread(fpath);
            code=regexprep(code,'(?<=classdef *)\w+',className,'ignorecase','once');
            
            % checking if exists, and if the same, then ignore write.
            % otherwise delete old.
            if(exist(fname,'file'))
                oldcode=fileread(fname);
                if(strcmp(oldcode,code))
                    return;
                end
                delete(fname);
            end
            
            % write all.
            fid=fopen(fname,'a');
            fprintf(fid,"%s",code);
            fclose(fid);
        end
        
        function [id]=PathToLVID(fpath)
            id=['P',lvport_hash(lower(fpath)),'C'];
        end
    end
end

