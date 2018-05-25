function [txt] = removeHtmlTags(txt)
    %txt=regexprep(txt,'\n','@@####@@');
    txt=regexprep(txt,'(<[^\/]\s*.+?([^\w]>))|(<\/\s*.+?>)','');
    %txt=regexprep(txt,'@@####@@','\n');
%     starts=regexp(txt,'<\s*\w+');
%     ends=regexp(txt,'<\/\s*.+?>');
%     locs=[starts,ends];
%     op=[ones(size(starts)),zeros(size(ends))];
%     [locs,sidx]=sort(locs);
%     op=op(sidx);
%     rmat=[];
%     for i=1:length(starts)
%         rmat=[rmat,starts(i):ends(i)];
%     end
%     txt(rmat(:))=[];
end

