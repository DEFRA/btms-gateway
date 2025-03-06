#!/bin/bash

ENDPOINT_URL=http://localhost:4966

SNS_ARN=arn:aws:sns:eu-west-2:000000000000
SQS_ARN=arn:aws:sqs:eu-west-2:000000000000

export AWS_ENDPOINT_URL=$ENDPOINT_URL
export AWS_REGION=eu-west-2
export AWS_DEFAULT_REGION=eu-west-2
export AWS_ACCESS_KEY_ID=local
export AWS_SECRET_ACCESS_KEY=local

ADF=alvs_decision_fork.fifo
ADR=alvs_decision_route.fifo
AEF=alvs_error_fork.fifo
AER=alvs_error_route.fifo
CCF=customs_clearance_fork.fifo
CCR=customs_clearance_route.fifo
CEF=customs_error_fork.fifo
CER=customs_error_route.fifo
CFF=customs_finalisation_fork.fifo
CFR=customs_finalisation_route.fifo

queueName=$ADF
echo "[$(date +"%T")] Started setting up queue: $queueName"
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $queueName
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $queueName
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$queueName --protocol sqs --notification-endpoint $SQS_ARN:$queueName --attributes '{"RawMessageDelivery": "true"}'
echo "[$(date +"%T")] Finished setting up queue: $queueName"

queueName=$ADR
echo "[$(date +"%T")] Started setting up queue: $queueName"
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $queueName
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $queueName
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$queueName --protocol sqs --notification-endpoint $SQS_ARN:$queueName --attributes '{"RawMessageDelivery": "true"}'
echo "[$(date +"%T")] Finished setting up queue: $queueName"

queueName=$AEF
echo "[$(date +"%T")] Started setting up queue: $queueName"
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $queueName
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $queueName
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$queueName --protocol sqs --notification-endpoint $SQS_ARN:$queueName --attributes '{"RawMessageDelivery": "true"}'
echo "[$(date +"%T")] Finished setting up queue: $queueName"

queueName=$AER
echo "[$(date +"%T")] Started setting up queue: $queueName"
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $queueName
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $queueName
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$queueName --protocol sqs --notification-endpoint $SQS_ARN:$queueName --attributes '{"RawMessageDelivery": "true"}'
echo "[$(date +"%T")] Finished setting up queue: $queueName"

queueName=$CCF
echo "[$(date +"%T")] Started setting up queue: $queueName"
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $queueName
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $queueName
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$queueName --protocol sqs --notification-endpoint $SQS_ARN:$queueName --attributes '{"RawMessageDelivery": "true"}'
echo "[$(date +"%T")] Finished setting up queue: $queueName"

queueName=$CCR
echo "[$(date +"%T")] Started setting up queue: $queueName"
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $queueName
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $queueName
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$queueName --protocol sqs --notification-endpoint $SQS_ARN:$queueName --attributes '{"RawMessageDelivery": "true"}'
echo "[$(date +"%T")] Finished setting up queue: $queueName"

queueName=$CEF
echo "[$(date +"%T")] Started setting up queue: $queueName"
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $queueName
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $queueName
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$queueName --protocol sqs --notification-endpoint $SQS_ARN:$queueName --attributes '{"RawMessageDelivery": "true"}'
echo "[$(date +"%T")] Finished setting up queue: $queueName"

queueName=$CER
echo "[$(date +"%T")] Started setting up queue: $queueName"
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $queueName
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $queueName
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$queueName --protocol sqs --notification-endpoint $SQS_ARN:$queueName --attributes '{"RawMessageDelivery": "true"}'
echo "[$(date +"%T")] Finished setting up queue: $queueName"

queueName=$CFF
echo "[$(date +"%T")] Started setting up queue: $queueName"
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $queueName
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $queueName
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$queueName --protocol sqs --notification-endpoint $SQS_ARN:$queueName --attributes '{"RawMessageDelivery": "true"}'
echo "[$(date +"%T")] Finished setting up queue: $queueName"

queueName=$CFR
echo "[$(date +"%T")] Started setting up queue: $queueName"
aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $queueName
aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $queueName
aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$queueName --protocol sqs --notification-endpoint $SQS_ARN:$queueName --attributes '{"RawMessageDelivery": "true"}'
echo "[$(date +"%T")] Finished setting up queue: $queueName"


declare -a arr=(
        "alvs_decision_fork.fifo"
        "alvs_decision_route.fifo"
        "alvs_error_fork.fifo"
        "alvs_error_route.fifo"
        "customs_clearance_fork.fifo"
        "customs_clearance_route.fifo"
        "customs_error_fork.fifo"
        "customs_error_route.fifo"
        "customs_finalisation_fork.fifo"
        "customs_finalisation_route.fifo"
    )

SNS_ARN=arn:aws:sns:eu-west-2:000000000000
SQS_ARN=arn:aws:sqs:eu-west-2:000000000000

for queueName in "${arr[@]}"
do
   timestamp="$(date +"%T")"
   echo "[$(date +"%T")] Started setting up queue: $queueName"
   aws --endpoint-url=$ENDPOINT_URL sns create-topic --attributes FifoTopic=true --name $queueName
   aws --endpoint-url=$ENDPOINT_URL sqs create-queue --attributes FifoQueue=true --queue-name $queueName
   aws --endpoint-url=$ENDPOINT_URL sns subscribe --topic-arn $SNS_ARN:$queueName --protocol sqs --notification-endpoint $SQS_ARN:$queueName --attributes '{"RawMessageDelivery": "true"}'
   echo "[$(date +"%T")] Finished setting up queue: $queueName"
done
