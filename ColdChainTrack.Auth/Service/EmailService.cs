using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ColdChainTrack.Auth.Service
{
    public class EmailService
    {
        Logger logger = LogManager.GetLogger("SendMail-log");

        public class EmailConfig
        {
            public string SmtpServer { get; set; }
            public string SmtpUser { get; set; }
            public string SmtpPass { get; set; }
            public int SmtpPort { get; set; }
            public int SSLPort { get; set; }
            public bool UseSSL { get; set; }
            public bool IsBodyHtml { get; set; }
            public string Recipients { get; set; }
            public string Cc { get; set; }
            public string Bcc { get; set; }
            public string DisplayName { get; set; }
            public string ReplyTo { get; set; }
        }

        public EmailConfig SetEmailConfig()
        {
            EmailConfig EmailConfig = new EmailConfig();

            EmailConfig.SmtpServer = Properties.Settings.Default.smtpServer;
            EmailConfig.SmtpUser = Properties.Settings.Default.smtpUser;
            EmailConfig.SmtpPass = Properties.Settings.Default.smtpPass;
            EmailConfig.DisplayName = Properties.Settings.Default.smtpDisplayName;
            EmailConfig.ReplyTo = Properties.Settings.Default.smtpReplyTo;
            EmailConfig.SSLPort = Properties.Settings.Default.sslPort;
            EmailConfig.SmtpPort = Properties.Settings.Default.smtpPort;
            EmailConfig.UseSSL = Properties.Settings.Default.EnableSsl;
            EmailConfig.IsBodyHtml = Properties.Settings.Default.IsBodyHtml;


            return EmailConfig;
        }

        public async Task<string> SendEmail_WithTemplate(string Subject, string ProjectName, string TemplateName,
            List<string> ReplaceValues, string Recipients, string Cc, string Bcc, string Attach1 = null, string Attach2 = null,
            string PassBookFile = null, string Passbook = null, string Pdf = null)
        {
            var errorMessage = "";
            try
            {
                //Setear Configuracion Email
                EmailConfig MyEmailConfig = SetEmailConfig();
                //string PassBookFile = CurrentYear + IdTicket.Replace("-", "") + ".pkpass"; //Properties.Settings.Default.PassbookFolder +

                MemoryStream pk = new MemoryStream();
                MemoryStream pdfFile = new MemoryStream();

                MailMessage mail = new MailMessage();
                SmtpClient EmailClient = new SmtpClient(MyEmailConfig.SmtpServer);

                if (Passbook != null)
                {
                    using (var client = new WebClient())
                    {
                        try
                        {
                            Uri PassbookUri = new Uri(Passbook);
                            pk = new MemoryStream(client.DownloadData(PassbookUri));
                            client.Dispose();
                        }
                        catch (Exception ex)
                        {
                            errorMessage += ex.Message + " | ";
                        }

                    }
                }

                if (Pdf != null)
                {
                    using (var client = new WebClient())
                    {
                        try
                        {
                            Uri PdfUri = new Uri(Pdf);
                            pdfFile = new MemoryStream(client.DownloadData(PdfUri));
                            client.Dispose();
                        }
                        catch (Exception ex)
                        {
                            errorMessage += ex.Message + " | ";
                        }

                    }
                }
                mail.From = new MailAddress(MyEmailConfig.ReplyTo, MyEmailConfig.DisplayName);
                string addresses = Recipients;
                string[] Multi = addresses.Split(',');
                foreach (string Multiemail in Multi)
                {
                    mail.To.Add(new MailAddress(Multiemail));
                    logger.Debug("MailTo: " + Multiemail);
                }
                string addr_cc = Cc;
                if (addr_cc != "" && addr_cc != null)
                {
                    string[] Multi_cc = addr_cc.Split(',');
                    foreach (string MultiCc in Multi_cc)
                    {
                        mail.CC.Add(new MailAddress(MultiCc));
                    }
                }

                string addr_bcc = Bcc;
                if (addr_bcc != "" && addr_bcc != null)
                {
                    string[] Multi_bcc = addr_bcc.Split(',');
                    foreach (string MultiBcc in Multi_bcc)
                    {
                        mail.Bcc.Add(new MailAddress(MultiBcc));
                    }
                }

                string addr_reply = MyEmailConfig.ReplyTo;
                string[] Multi_reply = addr_reply.Split(',');
                foreach (string MultiReply in Multi_reply)
                {
                    mail.ReplyToList.Add(new MailAddress(MultiReply));
                }

                mail.Subject = Subject;
                mail.IsBodyHtml = MyEmailConfig.IsBodyHtml;
                //Agregar contenido del Template
                string TemplatePath = HttpContext.Current.Server.MapPath("~/") + "Templates\\" + ProjectName + "\\" + TemplateName + ".html";
                logger.Debug("Template:" + TemplatePath);
                mail.Body = System.IO.File.ReadAllText(TemplatePath);
                //Reemplazar las variables con Textos correspondientes.
                int n = 0;
                foreach (var text in ReplaceValues)
                {
                    n++;
                    string ForReplace = "Replace" + n + "Content";
                    logger.Debug(ForReplace + " " + text);
                    mail.Body = mail.Body.Replace(ForReplace, text);
                }

                mail.BodyEncoding = Encoding.UTF8;
                EmailClient.Credentials = new System.Net.NetworkCredential(MyEmailConfig.SmtpUser, MyEmailConfig.SmtpPass);
                EmailClient.EnableSsl = MyEmailConfig.UseSSL;
                if (MyEmailConfig.UseSSL == true)
                {
                    EmailClient.Port = MyEmailConfig.SSLPort;
                }
                else
                {
                    EmailClient.Port = MyEmailConfig.SmtpPort;
                }
                //Adjuntar Archivo 1.
                //if (Attach1 != null)
                //{
                //    //var Ticket_pdf = new MemoryStream();
                //    PDFService MyPDF = new PDFService();
                //    MemoryStream Ticket_pdf = new MemoryStream();

                //    Ticket_pdf = MyPDF.GenerateTicketPDF(Pdf);

                //    try
                //    {
                //        if (Ticket_pdf != null)
                //        {
                //            logger.Debug("Ticket_pdf no es null");
                //            Attachment attachment;
                //            MemoryStream ms = new MemoryStream(Ticket_pdf.GetBuffer());
                //            attachment = new Attachment(ms, "application/pdf");
                //            attachment.ContentDisposition.FileName = "Boleto.pdf";
                //            //attachment.ContentStream = new MemoryStream(Ticket_pdf.GetBuffer());
                //            mail.Attachments.Add(attachment);
                //            //Ticket_pdf.Close();
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        logger.Debug("Error Attach1: " + ex.Message);
                //    }
                //}
                //Adjuntar Archivo 2.
                if (Attach2 != null)
                {
                    string FilePath2 = Properties.Settings.Default.TemplatesPath + ProjectName + "\\attachments\\" + Attach2;
                    try
                    {
                        if (System.IO.File.Exists(FilePath2))
                        {
                            Attachment attachment;
                            attachment = new Attachment(FilePath2);
                            mail.Attachments.Add(attachment);
                        }
                    }
                    catch { }
                }
                //Adjuntar Archivo PassBook.
                if (PassBookFile != null)
                {
                    try
                    {
                        if (pk != null)
                        {
                            Attachment attachment;
                            attachment = new Attachment(pk, PassBookFile, "application/vnd.apple.pkpass");
                            mail.Attachments.Add(attachment);
                        }
                    }
                    catch { }
                }
                if (Attach1 != null)
                {
                    try
                    {
                        if (pdfFile != null)
                        {
                            Attachment attachment;
                            attachment = new Attachment(pdfFile, Attach1, "application/pdf");
                            mail.Attachments.Add(attachment);
                        }
                    }
                    catch { }
                }

                EmailClient.Send(mail);
                logger.Debug("Resultado: OK");
                return "OK";
            }
            catch (Exception ex)
            {
                logger.Debug("Resultado: ERROR" + ex.Message + " - " + ex.InnerException);
                return "ERROR: " + ex.Message;
            }
        }
    }
}