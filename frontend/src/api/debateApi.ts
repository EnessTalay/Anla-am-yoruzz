import apiClient from './apiClient';
import type { StartDebateRequest, StartDebateResponse, AnalyzeDebateRequest, AnalysisResultDto } from '../types/debate';

export const startDebate = async (payload: StartDebateRequest): Promise<StartDebateResponse> => {
  const response = await apiClient.post<StartDebateResponse>('/api/v1/debate/start', payload);
  return response.data;
};

export const analyzeDebate = async (debateId: string, payload: AnalyzeDebateRequest): Promise<AnalysisResultDto> => {
  const response = await apiClient.post<AnalysisResultDto>(`/api/v1/debate/${debateId}/analyze`, payload);
  return response.data;
};

export const getDebateResult = async (debateId: string): Promise<AnalysisResultDto> => {
  const response = await apiClient.get<AnalysisResultDto>(`/api/v1/debate/${debateId}/result`);
  return response.data;
};

export const updateActionStep = async (debateId: string, stepId: string): Promise<void> => {
  await apiClient.patch(`/api/v1/debate/${debateId}/steps/${stepId}`);
};
