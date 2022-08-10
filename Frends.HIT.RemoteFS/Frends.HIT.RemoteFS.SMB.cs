﻿using System.Text;
using Renci.SshNet;
using SharpCifs.Smb;

namespace Frends.HIT.RemoteFS;

public class SMB
{
    public static List<string> ListFiles(ListParams input, ServerConfiguration connection)
    {
        var connectionstring = Helpers.GetSMBConnectionString(
            server: connection.Address,
            username: connection.Username,
            password: connection.Password,
            domain: connection.Domain,
            path: input.Path,
            file: ""
        );
        try
        {
            var folder = new SmbFile(connectionstring);
            return folder.ListFiles().Select(x => x.GetName()).ToList();
        }
        catch (Exception e)
        {
            throw new Exception($"Error listing files with user {connection.Username} (smb://{connection.Domain};{connection.Username}@{connection.Address}/{input.Path}) {e.Message}");
        }

    }
    
    public static string ReadFile(ReadParams input, ServerConfiguration connection)
    {
        Encoding encType = Helpers.EncodingFromEnum(input.Encoding);
        
        var file = new SmbFile(Helpers.GetSMBConnectionString(
            server: connection.Address,
            username: connection.Username,
            password: connection.Password,
            domain: connection.Domain,
            path: input.Path,
            file: input.File
        ));

        if (file.Exists())
        {
            var readStream = file.GetInputStream();
            var memStream = new MemoryStream();
            
            ((Stream)readStream).CopyTo(memStream);
            readStream.Dispose();
            
            return encType.GetString(memStream.ToArray());
        }
        
        throw new Exception($"File {input.Path}/{input.File} does not exist");
    }

    public static void WriteFile(WriteParams input, ServerConfiguration connection)
    {
        Encoding encType = Helpers.EncodingFromEnum(input.Encoding);
        
        var file = new SmbFile(Helpers.GetSMBConnectionString(
            server: connection.Address,
            username: connection.Username,
            password: connection.Password,
            domain: connection.Domain,
            path: input.Path,
            file: input.File
        ));

        if (file.Exists())
        {
            if (!input.Overwrite)
            {
                throw new Exception($"File {input.Path}/{input.File} already exists and Overwrite is not enabled");
            }
            file.Delete();
        }
        
        file.CreateNewFile();
        
        var writeStream = file.GetOutputStream();
        writeStream.Write(encType.GetBytes(input.Content));
        writeStream.Dispose();
    }

    public static void CreateDir(CreateDirParams input, ServerConfiguration connection)
    {
        var folder = new SmbFile(Helpers.GetSMBConnectionString(
            server: connection.Address,
            username: connection.Username,
            password: connection.Password,
            domain: connection.Domain,
            path: input.Path
        ));

        if (folder.IsFile())
        {
            throw new Exception("The path cannot be created because there is a file with the same name present");
        }

        if (!folder.IsDirectory())
        {
            if (input.Recursive)
            {
                folder.Mkdirs();
            }
            else
            {
                folder.Mkdir();
            }    
        }
    }
    
    public static void DeleteFile(DeleteParams input, ServerConfiguration connection)
    {
        var file = new SmbFile(Helpers.GetSMBConnectionString(
            server: connection.Address,
            username: connection.Username,
            password: connection.Password,
            domain: connection.Domain,
            path: input.Path,
            file: input.File
        ));

        file.Delete();
    }
}