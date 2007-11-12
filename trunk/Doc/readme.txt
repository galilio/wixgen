--------------------
WixGen utility v1.0
--------------------

WixGen - Утилита командной строки, которая позволяет формировать WiX скрипты (файлы .wxs) на основе шаблонов (.wxs_t).
Главная цель этой утилиты включать в .wxs файлы список файлов из каталогов по маске.


---------------------------
Параметры командной строки 
---------------------------
usage:  WixGen.exe [-?] [-nologo] [-wix3] [-outdir directory] [-t templateFile [-t templateFile] ...]

  -wix3               Output in WiX 3 format
  -D:<name>=<value>   Define value for given property. Use in code as ${name}.
  -t                  Template file name with extension .wix-t
  -noexcludes         Disable default excludes (vssver.scc)
  -outdir             Output directory for .wix files
  -nologo             Skip printing tallow logo information
  -?                  This help information

-----------
Comment
-----------
Detail examples located in "Examples" directory.


-------------------
Example 1
-------------------

Template:
		<TDirectoryTree 
					 Id="Dir.MyWebsite" 
					 SourceDir="C:\Build\Binaries\_PublishedWebsites\MyWebSite" 
					 Name="MyWebsite" 
					 ComponentId="Cm.MyWebsite"
					 Guid="03A531A9-1111-4A6A-2222-3A0BF3039102">
		</TDirectoryTree>

Result:
      <Directory Id="Dir.MyWebsite" Name="MyWebsite">
        <Directory Name="bin" Id="Dir.bin.03A531A9">
          <Component Id="CM.bin.03A531A9" DiskId="1" Guid="f6a5efa9-acb3-446a-80c3-3a37f39b91a1" KeyPath="yes">
            <File Id="MyWebService.dll.cytorvdm.dll.f6a5efa9_acb3_446a_80c3_3a37f39b91a1" Name="MyWebService.dll" Source="$(var._BaseDir)\bin\MyWebService.dll" Vital="yes" />
            <File Id="MyWebService.pdb.jffx4voy.pdb.f6a5efa9_acb3_446a_80c3_3a37f39b91a1" Name="MyWebService.pdb" Source="$(var._BaseDir)\bin\MyWebService.pdb" Vital="yes" />
          </Component>
        </Directory>
      </Directory>


-------------------
Example 2
-------------------
Template:

<TDirectory Id="Dir1" Name="Directory1" ComponentId="Component1" Guid="3BA67D06-E0A3-4fd5-B39F-FF99CD6ACAE7">

	<TFiles SourceDir="C:\MyFilesForInstall" DiskId="1">
		<Include Name="*.asp"/>
		<Include Name="*.txt"/>
	</TFiles>

</TDirectory>

Result:

<Directory Id="Dir1" Name="Dir_001" LongName="Directory1">
	<Component Id="Component1" Guid="3BA67D06-E0A3-4fd5-B39F-FF99CD6ACAE7">
          <File Id="MyFile1.asp.0" Name="xxx_0.asp" LongName="MyFile1.asp" src="C:\MyFilesForInstall\MyFile1.asp" DiskId="1"/>
          <File Id="MyFile2.asp.1" Name="xxx_1.asp" LongName="MyFile2.asp" src="C:\MyFilesForInstall\MyFile2.asp" DiskId="1"/>
          <File Id="MyTextFile.txt.2" Name="xxx_2.txt" LongName="MyTextFile.txt" src="C:\MyFilesForInstall\MyTextFile.txt" DiskId="1"/>
	</Component>
</Directory>

----------------------------------------------------------------
