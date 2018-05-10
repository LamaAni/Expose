classdef CSComMessage <handle
    %CSCOMMESSAGE Summary of this class goes here
    %   Detailed explanation goes here

    methods
        function obj = CSComMessage(msg,type,map,compareTo)
            if(isa(msg,'string'))
               msg=char(msg); 
            end
            if(ischar(msg))
                obj.Message=msg;
            else
                obj.Message=[];
            end
            
            if(isnumeric(type))
                obj.MessageType=type;
            else
                obj.MessageType=CSComMessageType.Data;
            end
            
            if(exist('map','var'))
                if(exist('compareTo','var'))
                    obj.Namepaths=CSComMessageNamepathData.ToNamepathDataMap(map,compareTo);
                else
                    obj.Namepaths=CSComMessageNamepathData.ToNamepathDataMap(map);
                end
            end
        end
                
    end
    
    properties(SetAccess = protected)
        Message=[];
        MessageType=CSComMessageType.Data;
        Namepaths=[];
    end
    
    methods
        function msg=ToNetObject(obj)
            % collecting data.
            if(~isempty(obj.Namepaths))

                vals=obj.Namepaths.values;
                data=NET.createArray('CSCom.NPMessageNamepathData',length(vals));
                for i=1:length(vals)
                    npd=vals{i};
                    csnpd=CSCom.NPMessageNamepathData();
                    csnpd.Value=npd.Value;
                    csnpd.Namepath=npd.Namepath;
                    csnpd.Idxs=npd.Idxs;
                    csnpd.Size=npd.Size;
                    data(i)=csnpd;
                end
            else
                data=NET.createArray('CSCom.NPMessageNamepathData',0);
            end
            mtype=CSComMessageType.Error;
            if(~isempty(obj.MessageType))
                mtype=obj.MessageType;
            end
            
            msg=CSCom.NPMessage(int32(mtype),data,char(obj.Message));
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
            
            vals=obj.Namepaths.values;
            for i=1:length(vals)
                npd=vals{i};
                if(hasSource && map.isKey(npd.Namepath))
                    % need to update the source.
                    val=npd.GetValue(map(npd.Namepath));
                else
                    val=npd.GetValue();
                end
                o=ExposeMapper.update(o,npd.Namepath,val);
            end
        end        
    end
    
    methods(Static)
        function [o]=FromNetObject(nobj)
            % need to convert message to message map.
            % then send message.
            infos=nobj.NamePaths;
            map=containers.Map();
            for i=1:infos.Length
                info=infos(i);
                npd=CSComMessageNamepathData(...
                    CSCom.NetValueToRawData(info.Namepath),...
                    CSCom.NetValueToRawData(info.Value),...
                    CSCom.NetValueToRawData(info.Size),...
                    CSCom.NetValueToRawData(info.Idxs));
                map(npd.Namepath)=npd;
            end
            
            o=CSComMessage(CSCom.NetValueToRawData(nobj.Message),...
                CSComMessageType(int32(nobj.MessageType)),...
                map);    
        end
    end
        
end

