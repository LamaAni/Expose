% Calls to include all the code in the associated libreries of Zav\Code
% in the dropbox.
function IncludeLib(fpath)
    if(~exist('fpath','var'))
        fpath=mfilename('fullpath');
        fpath=fileparts(fpath);
    end
    dirlist=getAllDirectories(fpath);
    dirlist(end+1)={fpath};
    for i=1:length(dirlist)
        if(pathHasBeenAdded(dirlist{i}))
            continue;
        end
        addpath(dirlist{i});
        disp(['Path added: ',dirlist{i}]);
    end
end

function dirList = getAllDirectories(dirName)
  dirData = dir(dirName);      %# Get the data for the current directory
  dirIndex = [dirData.isdir];  %# Find the index for directories
  dirIndex(1)=0;dirIndex(2)=0; % remove '.' '..'.
  dirList = fullfile(dirName,{dirData(dirIndex).name}');  %# Get a list of the files
  if ~isempty(dirList)
    % search subdirs.
    for iDir=1:length(dirList)                 %# Loop over valid subdirectories
        dirList = [dirList; getAllDirectories(dirList{iDir})];  %# Recursively call getAllFiles
    end
  end
end

function [onPath]=pathHasBeenAdded(Folder)
    pathCell = regexp(path, pathsep, 'split');
    if ispc  % Windows is not case-sensitive
      onPath = any(strcmpi(Folder, pathCell));
    else
      onPath = any(strcmp(Folder, pathCell));
    end
end