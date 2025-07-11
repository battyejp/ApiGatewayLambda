FROM amazon/aws-cli:latest

# Install curl and other utilities
RUN yum update -y && yum install -y curl zip unzip && yum clean all

# Create app directory
WORKDIR /app

# Copy deployment script
COPY deploy.sh /app/deploy.sh

# Make the script executable
RUN chmod +x /app/deploy.sh

# Copy the lambda deployment package
COPY lambda.zip /app/lambda.zip

# Set the entrypoint
ENTRYPOINT ["/app/deploy.sh"]
