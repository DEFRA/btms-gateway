#!/usr/bin/env bash

cp /etc/hosts hosts

sed -i '$ a 10.62.146.246 t2.secure.services.defra.gsi.gov.uk' hosts

cp hosts /etc/hosts

#dotnet BtmsGateway.dll
