#!/bin/bash

ENDPOINT_URL=http://sqs.eu-west-2.localhost.localstack.cloud:4566

export AWS_ENDPOINT_URL=$ENDPOINT_URL
export AWS_REGION=eu-west-2
export AWS_DEFAULT_REGION=eu-west-2
export AWS_ACCESS_KEY_ID=local
export AWS_SECRET_ACCESS_KEY=local

# SNS/SQS topics, queues, subscriptions

CCR=customs_clearance_request.fifo
CFN=customs_finalisation_notification.fifo
CEN=customs_error_notification.fifo
ADN=alvs_decision_notification.fifo
AEN=alvs_error_notification.fifo
ICDR=trade_imports_inbound_customs_declaration_received.fifo

aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $CCR
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $CFN
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $CEN
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $ADN
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $AEN
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $ICDR

aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $CCR
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $CFN
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $CEN
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $ADN
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $AEN
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $ICDR

SNS_ARN=arn:aws:sns:eu-west-2:000000000000
SQS_ARN=arn:aws:sqs:eu-west-2:000000000000

aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$CCR --protocol sqs --notification-endpoint $SQS_ARN:$CCR --attributes '{"RawMessageDelivery": "true"}'
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$CFN --protocol sqs --notification-endpoint $SQS_ARN:$CFN --attributes '{"RawMessageDelivery": "true"}'
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$CEN --protocol sqs --notification-endpoint $SQS_ARN:$CEN --attributes '{"RawMessageDelivery": "true"}'
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$ADN --protocol sqs --notification-endpoint $SQS_ARN:$ADN --attributes '{"RawMessageDelivery": "true"}'
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$AEN --protocol sqs --notification-endpoint $SQS_ARN:$AEN --attributes '{"RawMessageDelivery": "true"}'
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$ICDR --protocol sqs --notification-endpoint $SQS_ARN:$ICDR --attributes '{"RawMessageDelivery": "true"}'
