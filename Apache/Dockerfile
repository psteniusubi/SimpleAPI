FROM ubuntu:24.04

RUN <<EOT bash
apt -y update
apt -y install apache2-bin libapache2-mod-auth-openidc media-types
EOT

#ADD https://github.com/psteniusubi/SimpleAPI.git /SimpleAPI
ADD . /SimpleAPI/Apache

WORKDIR /SimpleAPI/Apache

ENTRYPOINT [ "/bin/bash" ]
CMD [ "run-apache.sh" ]
