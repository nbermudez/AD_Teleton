﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

//Agregados
using System.Web.UI.HtmlControls;
using BL;
using System.Data;
using System.Text;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.HPSF;
using NPOI.POIFS.FileSystem;
using NPOI.SS.UserModel;

public partial class ReporteDadosAlta : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            List<string> permisos = (List<string>)Session["Permisos_usuario"];
            bool permisoEncontrado = false;
            Paciente pac = new Paciente();

            foreach (string rol in permisos)
            {
                if (rol.Equals("pRepAltas"))
                {
                    permisoEncontrado = true;
                    break;
                }
            }

            if (!permisoEncontrado)
            {
                //Si no tiene permiso redireccionamos
                //Response.Write("<script>alert('Usted no posee permisos suficientes para accesar a este recurso')</script>");
                Response.Redirect("NoAccess.aspx");
            }

            if (!IsPostBack)
            {
                if (!pac.isDoctor(Session.Contents["nombre_usuario"].ToString()))
                    cargarDoctores();
                else
                    cargarDoctor();
            }

            ceFechaFinal.SelectedDate = DateTime.Now;
            ceFechaInicio.SelectedDate = DateTime.Now;
        }
        catch (Exception error)
        {
            Session["Error_Msg"] = error.Message+"\n"+error.StackTrace;
            Response.Redirect("~/Error.aspx", true);
        }
    }



    private void cargarDoctores()
    {
        try
        {
            BL.Empleados doctores = new BL.Empleados();
            Usuarios usuarios = new Usuarios();
            Paciente pac = new Paciente();

            List<string> usuariosTemp = usuarios.RetrieveUserNames();
            List<string> usersDocs = new List<string>();
            List<long> ids = new List<long>();
            List<string> nombres = new List<string>();
            List<string> apellido = new List<string>();
            List<string> segundoApellido = new List<string>();

            foreach (string doc in usuariosTemp)
            {
                if (pac.isDoctor(doc))
                {
                    ids.Add(usuarios.retriveEmpId(doc));
                    usersDocs.Add(doc);
                }
            }

            foreach (long codigo in ids)
            {
                nombres.Add(doctores.obtenerNombresDoctores(codigo));
                apellido.Add(doctores.obtenerApellidoDoctores(codigo));
                segundoApellido.Add(doctores.obtenerSegundoApellidoDoctores(codigo));
            }

            ListItem temporal = new ListItem();
            temporal.Text = "--- Todos ---";
            temporal.Value = "todos";
            temporal.Selected = true;
            ddlDoctor.Items.Add(temporal);

            for (int i = 0; i < nombres.Count; i++)
            {
                ListItem item = new ListItem();
                item.Text = nombres[i] + " " + apellido[i] + " " + segundoApellido[i];
                item.Value = usersDocs[i].ToString();
                ddlDoctor.Items.Add(item);
            }
        }
        catch (Exception error)
        {
            Session["Error_Msg"] = error.Message;
            Response.Redirect("~/Error.aspx", true);
        }
    }

    private void cargarDoctor()
    {
        BL.Empleados doctor = new BL.Empleados();
        int codigo = Convert.ToInt32(Session["id_empleado"].ToString());
        string nombre = doctor.obtenerNombre(codigo);

        ListItem item = new ListItem();
        item.Text = nombre;
        item.Value = Convert.ToString(Session.Contents["nombre_usuario"]);
        item.Selected = true;
        ddlDoctor.Items.Add(item);
        ddlDoctor.Visible = false;
        lblDoctor.Visible = false;
    }


    protected void btEjecutar_Click(object sender, EventArgs e)
    {
        try
        {
            SeguimientoPacientes segPacientes = new SeguimientoPacientes();
            int centroId = (int)long.Parse(Session["Centro_idNum"].ToString());

            int yy = int.Parse(this.txtFechaInicio.Text.Substring(6, 4));
            int mm = int.Parse(this.txtFechaInicio.Text.Substring(3, 2));
            int dd = int.Parse(this.txtFechaInicio.Text.Substring(0, 2));
            DateTime fechaIni = new DateTime(yy, mm, dd);

            yy = int.Parse(this.txtFechaFinal.Text.Substring(6, 4));
            mm = int.Parse(this.txtFechaFinal.Text.Substring(3, 2));
            dd = int.Parse(this.txtFechaFinal.Text.Substring(0, 2));
            DateTime fechaFin = new DateTime(yy, mm, dd);

            gvSeguimientoPaciente.DataSource = segPacientes.ReporteDadosAltaporDoctor(fechaIni, fechaFin, centroId, ddlDoctor.SelectedValue);
            gvSeguimientoPaciente.DataBind();

            btExportar.Visible = true;
        }
        catch (Exception error)
        {
            Session["Error_Msg"] = error.Message;
            Response.Redirect("~/Error.aspx", true);
        }

    }

    protected void btExportar_Click(object sender, EventArgs e)
    {
        /*string filename = "Export.xls";
        Response.ContentType = "application/vnd.ms-excel";
        Response.AddHeader("Content-Disposition", String.Format("attachment;filename={0}", filename));
        Response.Clear();

        inicializarLibro();
        generarDatos();
        
        Response.BinaryWrite(escribirStream().GetBuffer());
        Response.End();*/

        exportToExcel("Export.xls", gvSeguimientoPaciente);
    }

    private void exportToExcel(string nameReport, GridView fuente)
    {
        HttpResponse response = Response;
        StringWriter sw = new StringWriter();
        HtmlTextWriter htw = new HtmlTextWriter(sw);
        Page pageToRender = new Page();
        HtmlForm form = new HtmlForm();
        form.Controls.Add(fuente);
        pageToRender.Controls.Add(form);
        response.Clear();
        response.Buffer = true;
        response.ContentType = "application/vnd.ms-excel";
        response.AddHeader("Content-Disposition", "attachment;filename=" + nameReport);
        response.Charset = "UTF-8";
        response.ContentEncoding = Encoding.Default;
        pageToRender.RenderControl(htw);
        response.Write(sw.ToString());
        response.End();
    }




}