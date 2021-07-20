#!/bin/sh

# Login to AWS using AWS SSO
#aws sso login --profile workbench

export ACCOUNT_ID=$(aws sts get-caller-identity --output text --query Account)
export AWS_REGION='eu-west-1'

# Create Amazon EKS Cluster
eksctl create cluster -f cluster.yaml
eksctl create iamidentitymapping --cluster awsdemo-hpa --arn arn:aws:iam::036828455238:role/aws-reserved/sso.amazonaws.com/eu-west-1/AWSReservedSSO_AWSAdministratorAccess_b3648c2b53da13ae --group system:masters --username admin

# Install Cluster Auto Scaling
aws iam create-policy   \
  --policy-name AWSDemoHPA-KubernetesAutoScalingPolicy \
  --policy-document file://cluster-autoscaler-policy.json

eksctl create iamserviceaccount \
    --name cluster-autoscaler \
    --namespace kube-system \
    --cluster awsdemo-hpa \
    --attach-policy-arn "arn:aws:iam::${ACCOUNT_ID}:policy/AWSDemoHPA-KubernetesAutoScalingPolicy" \
    --approve \
    --override-existing-serviceaccounts

kubectl apply -f cluster-autoscaler.yaml
kubectl -n kube-system annotate deployment.apps/cluster-autoscaler cluster-autoscaler.kubernetes.io/safe-to-evict="false"


# Install Kubernetes CloudWatch Adapter
aws iam create-policy   \
  --policy-name AWSDemoHPA-KubernetesCloudWatchAdapterPolicy \
  --policy-document file://k8s-cloudwatch-adapter-policy.json
  
eksctl create iamserviceaccount \
    --name k8s-cloudwatch-adapter \
    --namespace custom-metrics \
    --cluster awsdemo-hpa \
    --attach-policy-arn "arn:aws:iam::${ACCOUNT_ID}:policy/AWSDemoHPA-KubernetesCloudWatchAdapterPolicy" \
    --approve \
    --override-existing-serviceaccounts
kubectl apply -f k8s-cloudwatch-adapter.yaml

kubectl get --raw "/apis/external.metrics.k8s.io/v1beta1/namespaces/default/orders-queue-length" | jq '.'
