# -*- mode: ruby -*-
# vi: set ft=ruby :

Vagrant.configure("2") do |config|
  config.vm.box = "opentable/win-2012r2-standard-amd64-nocm"

  config.vm.provision "shell", inline: <<-SHELL
    iwr https://chocolatey.org/install.ps1 -UseBasicParsing | iex
    choco install -y dotnet4.5.2
    choco install -y microsoft-build-tools
    choco install -y visualfsharptools
  SHELL
end
