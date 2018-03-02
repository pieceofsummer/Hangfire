﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Hangfire.Dashboard.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    #line 2 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
    using Hangfire;
    
    #line default
    #line hidden
    
    #line 3 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
    using Hangfire.Dashboard;
    
    #line default
    #line hidden
    
    #line 4 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
    using Hangfire.Dashboard.Pages;
    
    #line default
    #line hidden
    
    #line 5 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
    using Hangfire.Dashboard.Resources;
    
    #line default
    #line hidden
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    internal partial class DeletedJobsPage : RazorPage
    {
#line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");







            
            #line 7 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
  
    Layout = new LayoutPage(Strings.DeletedJobsPage_Title);

    int from, perPage;

    int.TryParse(Query("from"), out from);
    int.TryParse(Query("count"), out perPage);

    var monitor = Storage.GetMonitoringApi();
    var pager = new Pager(from, perPage, monitor.DeletedListCount());
    var jobs = monitor.DeletedJobs(pager.FromRecord, pager.RecordsPerPage);


            
            #line default
            #line hidden
WriteLiteral("\r\n<div class=\"row\">\r\n    <div class=\"col-md-3\">\r\n        ");


            
            #line 22 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
   Write(Html.JobsSidebar());

            
            #line default
            #line hidden
WriteLiteral("\r\n    </div>\r\n    <div class=\"col-md-9\">\r\n        <h1 class=\"page-header\">");


            
            #line 25 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                           Write(Strings.DeletedJobsPage_Title);

            
            #line default
            #line hidden
WriteLiteral("</h1>\r\n\r\n");


            
            #line 27 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
         if (pager.TotalPageCount == 0)
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"alert alert-info\">\r\n                ");


            
            #line 30 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
           Write(Strings.DeletedJobsPage_NoJobs);

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 32 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
        }
        else
        {

            
            #line default
            #line hidden
WriteLiteral("            <div class=\"js-jobs-list\">\r\n                <div class=\"btn-toolbar b" +
"tn-toolbar-top\">\r\n");


            
            #line 37 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                     if (!IsReadOnly)
                    {

            
            #line default
            #line hidden
WriteLiteral("                        <button class=\"js-jobs-list-command btn btn-sm btn-primar" +
"y\"\r\n                                data-url=\"");


            
            #line 40 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                     Write(Url.To("/jobs/deleted/requeue"));

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                data-loading-text=\"");


            
            #line 41 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                              Write(Strings.Common_Enqueueing);

            
            #line default
            #line hidden
WriteLiteral("\"\r\n                                disabled=\"disabled\">\r\n                        " +
"    <span class=\"glyphicon glyphicon-repeat\"></span>\r\n                          " +
"  ");


            
            #line 44 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                       Write(Strings.Common_RequeueJobs);

            
            #line default
            #line hidden
WriteLiteral("\r\n                        </button>\r\n");


            
            #line 46 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                    }

            
            #line default
            #line hidden
WriteLiteral("                    ");


            
            #line 47 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
               Write(Html.PerPageSelector(pager));

            
            #line default
            #line hidden
WriteLiteral("\r\n                </div>\r\n                <div class=\"table-responsive\">\r\n       " +
"             <table class=\"table\">\r\n                        <thead>\r\n           " +
"                 <tr>\r\n");


            
            #line 53 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                 if (!IsReadOnly)
                                {

            
            #line default
            #line hidden
WriteLiteral("                                    <th class=\"min-width\">\r\n                     " +
"                   <input type=\"checkbox\" class=\"js-jobs-list-select-all\"/>\r\n   " +
"                                 </th>\r\n");


            
            #line 58 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                }

            
            #line default
            #line hidden
WriteLiteral("                                <th class=\"min-width\">");


            
            #line 59 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                 Write(Strings.Common_Id);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th>");


            
            #line 60 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                               Write(Strings.Common_Job);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                                <th class=\"align-right\">");


            
            #line 61 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                   Write(Strings.DeletedJobsPage_Table_Deleted);

            
            #line default
            #line hidden
WriteLiteral("</th>\r\n                            </tr>\r\n                        </thead>\r\n     " +
"                   <tbody>\r\n");


            
            #line 65 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                             foreach (var job in jobs)
                            {

            
            #line default
            #line hidden
WriteLiteral("                                <tr class=\"js-jobs-list-row ");


            
            #line 67 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                        Write(job.Value == null || !job.Value.InDeletedState ? "obsolete-data" : null);

            
            #line default
            #line hidden
WriteLiteral(" ");


            
            #line 67 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                                                                                                   Write(job.Value != null && job.Value.InDeletedState && job.Value != null ? "hover" : null);

            
            #line default
            #line hidden
WriteLiteral("\">\r\n");


            
            #line 68 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                     if (!IsReadOnly)
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <td>\r\n");


            
            #line 71 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                             if (job.Value == null || job.Value.InDeletedState)
                                            {

            
            #line default
            #line hidden
WriteLiteral("                                                <input type=\"checkbox\" class=\"js-" +
"jobs-list-checkbox\" name=\"jobs[]\" value=\"");


            
            #line 73 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                                                                                     Write(job.Key);

            
            #line default
            #line hidden
WriteLiteral("\"/>\r\n");


            
            #line 74 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                            }

            
            #line default
            #line hidden
WriteLiteral("                                        </td>\r\n");


            
            #line 76 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                    <td class=\"min-width\">\r\n                     " +
"                   ");


            
            #line 78 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                   Write(Html.JobIdLink(job.Key));

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 79 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                         if (job.Value != null && !job.Value.InDeletedState)
                                        {

            
            #line default
            #line hidden
WriteLiteral("                                            <span title=\"");


            
            #line 81 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                    Write(Strings.Common_JobStateChanged_Text);

            
            #line default
            #line hidden
WriteLiteral("\" class=\"glyphicon glyphicon-question-sign\"></span>\r\n");


            
            #line 82 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                        }

            
            #line default
            #line hidden
WriteLiteral("                                    </td>\r\n\r\n");


            
            #line 85 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                     if (job.Value == null)
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <td colspan=\"2\"><em>");


            
            #line 87 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                       Write(Strings.Common_JobExpired);

            
            #line default
            #line hidden
WriteLiteral("</em></td>\r\n");


            
            #line 88 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                    }
                                    else
                                    {

            
            #line default
            #line hidden
WriteLiteral("                                        <td class=\"word-break\">\r\n                " +
"                            ");


            
            #line 92 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                       Write(Html.JobNameLink(job.Key, job.Value.Job));

            
            #line default
            #line hidden
WriteLiteral("\r\n                                        </td>\r\n");



WriteLiteral("                                        <td class=\"align-right\">\r\n");


            
            #line 95 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                             if (job.Value.DeletedAt.HasValue)
                                            {
                                                
            
            #line default
            #line hidden
            
            #line 97 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                           Write(Html.RelativeTime(job.Value.DeletedAt.Value));

            
            #line default
            #line hidden
            
            #line 97 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                                                                             
                                            }

            
            #line default
            #line hidden
WriteLiteral("                                        </td>\r\n");


            
            #line 100 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                                    }

            
            #line default
            #line hidden
WriteLiteral("                                </tr>\r\n");


            
            #line 102 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
                            }

            
            #line default
            #line hidden
WriteLiteral("                        </tbody>\r\n                    </table>\r\n                <" +
"/div>\r\n\r\n                ");


            
            #line 107 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
           Write(Html.Paginator(pager));

            
            #line default
            #line hidden
WriteLiteral("\r\n            </div>\r\n");


            
            #line 109 "..\..\Dashboard\Pages\DeletedJobsPage.cshtml"
        }

            
            #line default
            #line hidden
WriteLiteral("    </div>\r\n</div>\r\n\r\n");


        }
    }
}
#pragma warning restore 1591
