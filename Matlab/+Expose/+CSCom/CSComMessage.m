classdef CSComMessage < Expose.Core.ExposeMessage
    %CSCOMMESSAGE Summary of this class goes here
    %   Detailed explanation goes here

    methods
        function obj = CSComMessage(txt,type,map,compareTo)
            import Expose.Core.*;
            import Expose.Map.*;
            import Expose.CSCom.*;
            
            if(exist('txt','var'))
                if(isa(txt,'string'))
                   txt=char(txt); 
                end

                if(ischar(txt))
                    obj.Text=txt;
                else
                    obj.Text=[];
                end
            else
                obj.Text=[];
            end
            
            if(exist('type','var') && isa(type,'Expose.Core.ExposeMessageType'))
                obj.MessageType=type;
            else
                obj.MessageType=ExposeMessageType.Warning;
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
    
    % anstract implementation
    methods
        
        % returns the value of the current message in matlab format.
        function [o]=Value(obj)
            o=obj.SetTo([]);
        end
        
        % sets the value of the current message to a specific object.
        function [o]=SetTo(obj,o)
            import Expose.Core.*;
            import Expose.Map.*;
            import Expose.CSCom.*;
            
            % update or make the object from the namepath.
            hasSource=true;
            if(~exist('o','var'))
                o=[];% new source object.
                hasSource=false;
            end
            
            if(hasSource)
                map=ExposeMapper.mapToCollection(o);
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
        
        % get the value of properties, in the form of an object from the
        % message namepath values.
        function [o]=GetFrom(obj,from)
            import Expose.Core.*;
            import Expose.Map.*;
            import Expose.CSCom.*;
            
            % creating the new map.
            if(isempty(obj.Namepaths))
                o=[];
                return;
            end
            o=struct();
            vals=obj.Namepaths.values;
            for i=1:length(vals)
                np=vals{i}.Namepath;
                val=ExposeMapper.getValueFromNamepath(from,np);
                o=ExposeMapper.update(o,np,val);
            end
        end
    end
    
    properties(SetAccess = protected)
        Namepaths=[];
    end
    
    methods
        % convert the current to a .net object, so it can be sent.
        function msg=ToNetObject(obj)
            import Expose.Core.*;
            import Expose.Map.*;
            import Expose.CSCom.*;
            
            % collecting data.
            if(~isempty(obj.Namepaths))
                vals=obj.Namepaths.values;
                data=NET.createArray('CSCom.NPMessageNamepathData',length(vals));
                for i=1:length(vals)
                    npd=vals{i};
                    csnpd=CSCom.NPMessageNamepathData();
                    csnpd.Value=npd.Value;
                    csnpd.Namepath=npd.Namepath;
%                     csnpd.Idxs=npd.Idxs;
%                     csnpd.Size=npd.Size;
                    data(i)=csnpd;
                end
            else
                data=NET.createArray('CSCom.NPMessageNamepathData',0);
            end
            mtype=ExposeMessageType.Error;
            if(~isempty(obj.MessageType))
                mtype=obj.MessageType;
            end
            msg=CSCom.NPMessage(int32(mtype),data,char(obj.Text));
        end
    
    end
    
    methods(Static)
        % convert a .net object to a matlab object so it can be handled.
        function [o]=FromNetObject(nobj)
            import Expose.Core.*;
            import Expose.Map.*;
            import Expose.CSCom.*;
            
            % need to convert message to message map.
            % then send message.
            if(isempty(nobj))
                o=[];
                return;
            end
            
            infos=nobj.NamePaths;
            map=containers.Map();
            for i=1:infos.Length
                info=infos(i);
                npd=CSComMessageNamepathData(...
                    CSCom.NetValueToRawData(info.Namepath),...
                    CSCom.NetValueToRawData(info.Value));
%                     CSCom.NetValueToRawData(info.Size),...
%                     CSCom.NetValueToRawData(info.Idxs));
                map(npd.Namepath)=npd;
            end
            
            o=CSComMessage(CSCom.NetValueToRawData(nobj.Text),...
                ExposeMessageType(int32(nobj.MessageType)),...
                map);
        end
    end
        
end

