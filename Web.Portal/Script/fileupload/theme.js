﻿(function ($) {
    "use strict";
    var teTagBef = '<tr class="file-preview-frame {frameClass}" id="{previewId}" data-fileindex="{fileindex}"' +
        ' data-template="{template}"', teContent = '<td class="kv-file-content">\n';
    $.fn.fileinputThemes.explorer = {
        layoutTemplates: {
            preview: '<div class="file-preview {class}">\n' +
            '    {close}' +
            '    <div class="{dropClass}">\n' +
            '    <table class="table table-bordered table-hover"><tbody class="file-preview-thumbnails">\n' +
            '    </tbody></table>\n' +
            '    <div class="clearfix"></div>' +
            '    <div class="file-preview-status text-center text-success"></div>\n' +
            '    <div class="kv-fileinput-error"></div>\n' +
            '    </div>\n' +
            '</div>',
            footer: '<td class="file-details-cell"><div class="explorer-caption" title="{caption}">{caption}</div> ' +
            '{size}{progress}</td><td class="file-actions-cell">{actions}</td>',
            actions: '<div class="file-upload-indicator" title="{indicatorTitle}">{indicator}</div>\n' +
            '{drag}\n' +
            '<div class="file-actions">\n' +
            '    <div class="file-footer-buttons">\n' +
            '        {upload} {delete} {zoom} {other} ' +
            '    </div>\n' +
            '</div>',
            zoomCache: '<tr style="display:none" class="kv-zoom-cache-theme"><td>' +
            '<table class="kv-zoom-cache">{zoomContent}</table></td></tr>'
        },
        previewMarkupTags: {
            tagBefore1: teTagBef + '>' + teContent,
            tagBefore2: teTagBef + ' title="{caption}" style="width:{width};height:{height};">' + teContent,
            tagAfter: '</td>\n{footer}</tr>\n'
        },
        previewSettings: {
            image: { height: "60px" },
            html: { width: "100px", height: "60px" },
            text: { width: "100px", height: "60px" },
            video: { width: "auto", height: "60px" },
            audio: { width: "auto", height: "60px" },
            flash: { width: "100%", height: "60px" },
            object: { width: "100%", height: "60px" },
            pdf: { width: "100px", height: "60px" },
            other: { width: "100%", height: "60px" }
        },
        frameClass: 'explorer-frame'
    };
})(window.jQuery);