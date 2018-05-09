classdef CSComMessage <handle
    %CSCOMMESSAGE Summary of this class goes here
    %   Detailed explanation goes here

    methods
        function obj = CSComMessage(msg,type,map,compareTo)
            if(ischar(msg))
                obj.Message=msg;
            else
                obj.Message=[];
            end
            
            if(isnumeric(type))
                obj.MessageType=type;
            else
                obj.MessageType=-1;
            end
            
            if(exist('compareTo','var'))
                obj.Namepaths=CSComMessageNamepathData.ToNamepathDataMap(map,compareTo);
            else
                obj.Namepaths=CSComMessageNamepathData.ToNamepathDataMap(map);
            end
        end
                
    end
    
    properties(SetAccess = protected)
        Message=[];
        MessageType=[];
        Namepaths=[];
    end
    
    methods
        function msg=ToNetObject(obj)
            % collecting data.
            for i=1:length(obj.Namepaths.values)
                npd=obj.Namepaths.values{i};
                csnpd=NPMessageNamepathData();
                csnpd.Value=npd.Value;
                csnps.Namepath=npd.Namepath;
                csnps.Idxs=npd.Idxs;
                csnps.Size=npd.Size;
            end
            mtype=8;
            if(~isempty(obj.MessageType))
                mtype=obj.MessageType;
            end
            
            msg=CSCom.NPMessage(mtype,csnpd,char(obj.Message));
        end
        
        function [o]=UpdateObject(obj,o)
            % update or make the object from the namepath.
            hasSource=true;
            if(~exist('o','var'))
                o=[];% new source object.
                hasSource=false;
            end
            
            if(hasSource)
                map=ObjectMap.mapToCollection(o);
            end
            
            for i=1:length(obj.Namepaths.keys)
                npd=obj.Namepaths.values(i);
                if(hasSource && map.isKey(npd.Namepath))
                    % need to update the source.
                    val=npd.GetValue(map(npd.Namepath));
                else
                    val=npd.GetValue();
                end
                ObjectMap.update(o,npd.Namepath,val);
            end
        end        
    end
    
    methods(Static)
        function [o]=FromNetObject(nobj)
            % need to convert message to message map.
            % then send message.
            infos=nobj.NamePaths;
            map=containers.Map();
            for i=1:length(infos)
                info=infos(i);
                npd=CSComMessageNamepathData(...
                    CSCom.NetValueToRawData(info.Namepath),...
                    CSCom.NetValueToRawData(info.Value),...
                    CSCom.NetValueToRawData(info.Size),...
                    CSCom.NetValueToRawData(info.Idxs));
                map(npd.Namepath)=npd;
            end
            
            o=CSComMessage(nobj.Message,...
                int32(nobj.MessageType),map);    
        end
    end
        
end

