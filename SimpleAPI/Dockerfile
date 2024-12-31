FROM mcr.microsoft.com/dotnet/sdk:8.0

ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_HTTP_PORTS=5001

#ADD https://github.com/psteniusubi/SimpleAPI.git /SimpleAPI
ADD . /SimpleAPI/SimpleAPI

CMD [ "dotnet", "run", "--no-launch-profile", "--project", "SimpleAPI/SimpleAPI" ]
